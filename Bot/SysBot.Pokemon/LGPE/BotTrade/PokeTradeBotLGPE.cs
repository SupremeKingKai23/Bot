using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Core.Searching;
using PKHeX.Drawing.PokeSprite;
using SysBot.Base;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsLGPE;

namespace SysBot.Pokemon;

public class PokeTradeBotLGPE(PokeTradeHub<PB7> hub, PokeBotState cfg) : PokeRoutineExecutor7LGPE(cfg)
{
    private readonly PokeTradeHub<PB7> Hub = hub;
    private readonly TradeSettings TradeSettings = hub.Config.Trade;
    private readonly TradeAbuseSettings AbuseSettings = hub.Config.TradeAbuse;

    public ICountSettings Counts => Hub.Config.Counts;

    /// <summary>
    /// Folder to dump received trade data to.
    /// </summary>
    /// <remarks>If null, will skip dumping.</remarks>
    private readonly IDumper DumpSetting = hub.Config.Folder;

    /// <summary>
    /// Synchronized start for multiple bots.
    /// </summary>
    public bool ShouldWaitAtBarrier { get; private set; }

    /// <summary>
    /// Tracks failed synchronized starts to attempt to re-sync.
    /// </summary>
    public int FailedBarrier { get; private set; }

    public override async Task MainLoop(CancellationToken token)
    {
        try
        {

            await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

            Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            RecentTrainerCache.SetRecentTrainer(sav);


            Log($"Starting main {nameof(PokeTradeBotLGPE)} loop.");
            await InnerLoop(sav, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log(e.Message);
        }

        Log($"Ending {nameof(PokeTradeBotLGPE)} loop.");
        await HardStop().ConfigureAwait(false);
    }

    public override async Task HardStop()
    {
        UpdateBarrier(false);
        await CleanExit(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task InnerLoop(SAV7b sav, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Config.IterateNextRoutine();
            var task = Config.CurrentRoutineType switch
            {
                PokeRoutineType.Idle => DoNothing(token),
                _ => DoTrades(sav, token),
            };
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                Log(e.Message);
                break;

            }
        }
    }

    private async Task DoNothing(CancellationToken token)
    {
        int waitCounter = 0;
        while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
        {
            if (waitCounter == 0)
                Log("No task assigned. Waiting for new task assignment.");
            waitCounter++;
            if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                await Click(B, 1_000, token).ConfigureAwait(false);
            else
                await Task.Delay(1_000, token).ConfigureAwait(false);
        }
    }

    private async Task DoTrades(SAV7b sav, CancellationToken token)
    {
        var type = Config.CurrentRoutineType;
        int waitCounter = 0;
        while (!token.IsCancellationRequested && Config.NextRoutineType == type)
        {
            var (detail, priority) = GetTradeData(type);
            if (detail is null)
            {
                await WaitForQueueStep(waitCounter++, token).ConfigureAwait(false);
                continue;
            }
            waitCounter = 0;

            detail.IsProcessing = true;
            string tradetype = $" ({detail.Type})";
            Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
            Hub.Config.Stream.StartTrade(this, detail, Hub);
            Hub.Queues.StartTrade(this, detail);

            await PerformTrade(sav, detail, type, priority, token).ConfigureAwait(false);
        }
    }

    private async Task WaitForQueueStep(int waitCounter, CancellationToken token)
    {
        if (waitCounter == 0)
        {
            // Updates the assets.
            Hub.Config.Stream.IdleAssets(this);
            Log("Nothing to check, waiting for new users...");
        }

        const int interval = 10;
        if (waitCounter % interval == interval - 1 && Hub.Config.AntiIdle)
            await Click(B, 1_000, token).ConfigureAwait(false);
        else
            await Task.Delay(1_000, token).ConfigureAwait(false);
    }

    protected virtual (PokeTradeDetail<PB7>? detail, uint priority) GetTradeData(PokeRoutineType type)
    {
        if (Hub.Queues.TryDequeue(type, out var detail, out var priority))
            return (detail, priority);
        if (Hub.Queues.TryDequeueLedy(out detail))
            return (detail, PokeTradePriorities.TierFree);
        return (null, PokeTradePriorities.TierFree);
    }

    private async Task PerformTrade(SAV7b sav, PokeTradeDetail<PB7> detail, PokeRoutineType type, uint priority, CancellationToken token)
    {
        PokeTradeResult result;
        try
        {
            result = await PerformLinkCodeTrade(sav, detail, token).ConfigureAwait(false);
            if (result == PokeTradeResult.Success)
                return;
        }
        catch (SocketException socket)
        {
            Log(socket.Message);
            result = PokeTradeResult.ExceptionConnection;
            HandleAbortedTrade(detail, type, priority, result);
            throw; // let this interrupt the trade loop. re-entering the trade loop will recheck the connection.
        }
        catch (Exception e)
        {
            Log(e.Message);
            result = PokeTradeResult.ExceptionInternal;
        }

        HandleAbortedTrade(detail, type, priority, result);
    }

    private void HandleAbortedTrade(PokeTradeDetail<PB7> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
    {
        detail.IsProcessing = false;
        if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
        {
            detail.IsRetry = true;
            Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
            detail.SendNotification(this, LanguageHelper.RequeueAttempt(Hub.Config.CurrentLanguage));
        }
        else
        {
            detail.SendNotification(this, LanguageHelper.SomethingHappened(Hub.Config.CurrentLanguage, result));
            detail.TradeCanceled(this, result);
        }
    }

    private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV7b sav, PokeTradeDetail<PB7> poke, CancellationToken token)
    {
        if (Hub.Config.Stream.CreateAssets)
        {
            await CreateLGLinkCodeSpriteEmbed(poke.LGPETradeCode);
        }

        UpdateBarrier(poke.IsSynchronized);
        poke.TradeInitialize(this);
        Hub.Config.Stream.EndEnterCode(this);
        var toSend = poke.TradeData;
        if (toSend.Species != 0)
        {
            if (Hub.Config.Legality.UseTradePartnerInfo)
            {
                toSend = UpdateToTradePartnerOT(poke);
            }
            await WriteBoxPokemon(toSend, 0, 0, token);
        }
        if (!await IsOnOverworldStandard(token))
        {
            await ExitTrade(true, token).ConfigureAwait(false);
            return PokeTradeResult.RecoverStart;
        }
        await Click(X, 2000, token).ConfigureAwait(false);
        Log("Opening Menu");
        while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) != menuscreen)
        {
            await Click(B, 2000, token);
            await Click(X, 2000, token);
        }
        Log("Selecting Communicate");
        await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
        while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) == waitingtotradescreen)
        {

            await Click(A, 1000, token);
            if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen2)
            {

                while (!await IsOnOverworldStandard(token))
                {

                    await Click(B, 1000, token);

                }
                await Click(X, 2000, token).ConfigureAwait(false);
                Log("Opening Menu");
                while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) != menuscreen)
                {
                    await Click(B, 2000, token);
                    await Click(X, 2000, token);
                }
                Log("Selecting Communicate");
                await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
            }


        }
        await Task.Delay(2000, token);
        Log("Selecting Faraway Connection");

        await SetStick(SwitchStick.RIGHT, 0, -30000, 0, token).ConfigureAwait(false);
        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
        await Click(A, 10000, token).ConfigureAwait(false);

        await Click(A, 1000, token).ConfigureAwait(false);

        // Wait for Barrier to trigger all bots simultaneously.
        WaitAtBarrierIfApplicable(token);

        await Task.Delay(Hub.Config.Timings.LGPEDistributionDelay, token);

        await EnterLinkCodeLG(poke, token);

        poke.TradeSearching(this);
        Log($"Searching for user {poke.Trainer.TrainerName}");
        await Task.Delay(3000, token);
        var btimeout = new Stopwatch();
        btimeout.Restart();

        while (await LGIsinwaitingScreen(token))
        {
            await Task.Delay(100, token);
            if (btimeout.ElapsedMilliseconds >= 45_000)
            {
                Log($"{poke.Trainer.TrainerName} not found");

                await ExitTrade(false, token);
                Hub.Config.Stream.EndEnterCode(this);
                return PokeTradeResult.NoTrainerFound;
            }
        }

        Log($"{poke.Trainer.TrainerName} Found");

        await Task.Delay(10000, token);

        var tradepartnersav = new SAV7b();
        var tradepartnersav2 = new SAV7b();
        ulong trainerNID;
        var tpsarray = await SwitchConnection.ReadBytesAsync(TradePartnerData, 0x168, token);
        tpsarray.CopyTo(tradepartnersav.Blocks.Status.Data);
        var tpsarray2 = await SwitchConnection.ReadBytesAsync(TradePartnerData2, 0x168, token);
        tpsarray2.CopyTo(tradepartnersav2.Blocks.Status.Data);

        if (tradepartnersav.OT != sav.OT)
        {
            trainerNID = GetFakeNID(tradepartnersav.OT, tradepartnersav.ID32);
            RecordUtil<PokeTradeBotSWSH>.Record($"Initiating\t{tradepartnersav.OT}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
            Log($"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID},Game: {tradepartnersav.Version}");
            poke.SendNotification(this, $"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID}, Game: {tradepartnersav.Version}");

            var partnerCheck = await CheckPartnerReputation(this, poke, trainerNID, tradepartnersav.OT, AbuseSettings, token);
            if (partnerCheck != PokeTradeResult.Success)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return partnerCheck;
            }
        }

        if (tradepartnersav2.OT != sav.OT)
        {
            trainerNID = GetFakeNID(tradepartnersav2.OT, tradepartnersav2.ID32);
            RecordUtil<PokeTradeBotSWSH>.Record($"Initiating\t{tradepartnersav2.OT}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
            Log($"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}");
            poke.SendNotification(this, $"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}, Game: {tradepartnersav.Version}");

            var partnerCheck = await CheckPartnerReputation(this, poke, trainerNID, tradepartnersav2.OT, AbuseSettings, token);
            if (partnerCheck != PokeTradeResult.Success)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return partnerCheck;
            }
        }

        if (poke.Type == PokeTradeType.Dump)
        {
            var result = await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);
            await ExitTrade(false, token).ConfigureAwait(false);
            return result;
        }

        if (poke.Type == PokeTradeType.Clone)
        {
            var result = await ProcessCloneTradeAsync(poke, sav, token);
            await ExitTrade(false, token);
            return result;
        }

        while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == Boxscreen)
        {
            await Click(A, 1000, token);
        }

        poke.SendNotification(this, "You have 15 seconds to select your trade pokemon");
        Log("Waiting on Trade Screen");

        await Task.Delay(15_000, token).ConfigureAwait(false);

        var tradeResult = await ConfirmAndStartTrading(poke, 0, token);
        if (tradeResult != PokeTradeResult.Success)
        {
            if (tradeResult == PokeTradeResult.TrainerLeft)
                Log("Trade canceled because trainer left the trade.");
            await ExitTrade(false, token).ConfigureAwait(false);
            return tradeResult;
        }

        if (token.IsCancellationRequested)
        {
            await ExitTrade(false, token).ConfigureAwait(false);
            return PokeTradeResult.ExceptionInternal;
        }

        //trade was successful
        var received = await ReadPokemon(GetSlotOffset(0, 0), token);

        // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
        if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
        {
            Log("User did not complete the trade.");
            await ExitTrade(false, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        // As long as we got rid of our inject in b1s1, assume the trade went through.
        Log("User completed the trade.");
        poke.TradeFinished(this, received);

        // Only log if we completed the trade.
        UpdateCountsAndExport(poke, received, toSend);

        // Log for Trade Abuse tracking.
        var tid = tradepartnersav.OT != sav.OT ? tradepartnersav.DisplayTID : tradepartnersav2.DisplayTID;
        var sid = tradepartnersav.OT != sav.OT ? tradepartnersav.DisplaySID : tradepartnersav2.DisplaySID;
        var nid = tid + sid;
        var partner = tradepartnersav.OT != sav.OT ? tradepartnersav.OT : tradepartnersav2.OT;
        LogSuccessfulTrades(poke, nid, partner);
        LogLGPEUserOT(poke.Trainer.ID, partner, tid, sid);

        // Still need to wait out the trade animation.
        for (var i = 0; i < 30; i++)
            await Click(B, 0_500, token).ConfigureAwait(false);

        await ExitTrade(false, token).ConfigureAwait(false);
        return PokeTradeResult.Success;
    }

    private void UpdateCountsAndExport(PokeTradeDetail<PB7> poke, PB7 received, PB7 toSend)
    {
        var counts = Hub.Config.Counts;
        if (poke.Type == PokeTradeType.Random)
            counts.AddCompletedDistribution();
        else if (poke.Type == PokeTradeType.Clone)
            counts.AddCompletedClones();
        else if (poke.Type == PokeTradeType.FixOT)
            counts.AddCompletedFixOTs();
        else if (poke.Type == PokeTradeType.SupportTrade)
            counts.AddCompletedSupportTrades();
        else if (poke.Type == PokeTradeType.SpecialRequest)
            counts.AddCompletedSpecialRequests();
        else
            counts.AddCompletedTrade();

        if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
        {
            var subfolder = poke.Type.ToString().ToLower();
            DumpPokemon(DumpSetting.DumpFolder, subfolder, received); // received by bot
            if (poke.Type is PokeTradeType.Specific or PokeTradeType.Clone or PokeTradeType.FixOT or PokeTradeType.SupportTrade or PokeTradeType.SpecialRequest)
                DumpPokemon(DumpSetting.DumpFolder, "traded", toSend); // sent to partner
        }
    }

    private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PB7> detail, int slot, CancellationToken token)
    {
        await Task.Delay(5_000, token);
        var offeredData = await SwitchConnection.ReadBytesAsync(OfferedPokemon, 0x104, token);
        var offeredPoke = new PB7(offeredData);
        if (BannedPokemonSpecies.Contains(offeredPoke.Species))
        {
            detail.SendNotification(this, "Trade evolution was offered, leaving the trade.");
            Log("Trade evolution detected, leaving trade.");
            return PokeTradeResult.TradeEvoDetected;
        }
        // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
        var oldEC = await Connection.ReadBytesAsync((uint)GetSlotOffset(0, slot), 8, token).ConfigureAwait(false);
        Log("Confirming and Initiating Trade");
        await Click(A, 3_000, token).ConfigureAwait(false);
        for (int i = 0; i < 10; i++)
        {

            if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == Boxscreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen)
                return PokeTradeResult.TrainerLeft;
            await Click(A, 1_500, token).ConfigureAwait(false);
        }

        var tradeCounter = 0;
        Log("Checking for received Pokemon in slot 1");
        while (true)
        {

            var newEC = await Connection.ReadBytesAsync((uint)GetSlotOffset(0, slot), 8, token).ConfigureAwait(false);
            if (!newEC.SequenceEqual(oldEC))
            {
                Log("Change detected in slot 1");
                await Task.Delay(15_000, token).ConfigureAwait(false);
                return PokeTradeResult.Success;
            }

            tradeCounter++;

            if (tradeCounter >= Hub.Config.Trade.TradeAnimationMaxDelaySeconds)
            {
                // If we don't detect a B1S1 change, the trade didn't go through in that time.
                Log("No change detected in slot 1");
                return PokeTradeResult.TrainerTooSlow;
            }

            if (await IsOnOverworldStandard(token))
                return PokeTradeResult.TrainerLeft;
            await Task.Delay(1000, token);
        }
    }

    private async Task<PokeTradeResult> ProcessCloneTradeAsync(PokeTradeDetail<PB7> detail, SAV7b sav, CancellationToken token)
    {
        detail.SendNotification(this, "Highlight the Pokemon in your box You would like Cloned up to 6 at a time! You have 5 seconds between highlights to move to the next pokemon.(The first 5 starts now!). If you would like to clone less than 6, remain on the same pokemon until the trade begins.");
        await Task.Delay(10_000, token);
        var offereddatac = await SwitchConnection.ReadBytesAsync(OfferedPokemon, 0x104, token);
        var offeredpbmc = new PB7(offereddatac);
        List<PB7> clonelist =
        [
            offeredpbmc
        ];
        detail.SendNotification(this, $"You added {(Species)offeredpbmc.Species} to the clone list");


        for (int i = 0; i < 6; i++)
        {
            await Task.Delay(5_000, token);
            var newoffereddata = await SwitchConnection.ReadBytesAsync(OfferedPokemon, 0x104, token);
            var newofferedpbm = new PB7(newoffereddata);
            if (clonelist.Any(z => SearchUtil.HashByDetails(z) == SearchUtil.HashByDetails(newofferedpbm)))
            {
                continue;
            }
            else
            {
                clonelist.Add(newofferedpbm);
                offeredpbmc = newofferedpbm;
                detail.SendNotification(this, $"You added {(Species)offeredpbmc.Species} to the clone list");
            }

        }

        var clonestring = new StringBuilder();
        foreach (var k in clonelist)
            clonestring.AppendLine($"{(Species)k.Species}");
        detail.SendNotification(this, "Pokemon to be Cloned", clonestring.ToString());

        detail.SendNotification(this, "Exiting Trade to inject clones, please reconnect using the same link code.");
        await ExitTrade(false, token);
        foreach (var g in clonelist)
        {
            await WriteBoxPokemon(g, 0, clonelist.IndexOf(g), token);
            await Task.Delay(1000, token);
        }
        await Click(X, 2000, token).ConfigureAwait(false);
        Log("Opening Menu");
        while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) != menuscreen)
        {
            await Click(B, 2000, token);
            await Click(X, 2000, token);
        }
        Log("Selecting Communicate");
        await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
        while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == menuscreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) == waitingtotradescreen)
        {

            await Click(A, 1000, token);
            if (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen || BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == savescreen2)
            {

                while (!await IsOnOverworldStandard(token))
                {

                    await Click(B, 1000, token);

                }
                await Click(X, 2000, token).ConfigureAwait(false);
                Log("Opening Menu");
                while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 4, token), 0) != menuscreen)
                {
                    await Click(B, 2000, token);
                    await Click(X, 2000, token);
                }
                Log("Selecting Communicate");
                await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
            }


        }
        await Task.Delay(2000, token);
        Log("Selecting Faraway Connection");

        await SetStick(SwitchStick.RIGHT, 0, -30000, 0, token).ConfigureAwait(false);
        await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
        await Click(A, 10000, token).ConfigureAwait(false);

        await Click(A, 1000, token).ConfigureAwait(false);
        await EnterLinkCodeLG(detail, token);
        detail.TradeSearching(this);
        Log($"Searching for user {detail.Trainer.TrainerName}");
        var btimeout = new Stopwatch();
        while (await LGIsinwaitingScreen(token))
        {
            await Task.Delay(100, token);
            if (btimeout.ElapsedMilliseconds >= 45_000)
            {
                detail.TradeCanceled(this, PokeTradeResult.NoTrainerFound);
                Log($"{detail.Trainer.TrainerName} not found");


                await ExitTrade(false, token);
                Hub.Config.Stream.EndEnterCode(this);
                return PokeTradeResult.NoTrainerFound;
            }
        }
        Log($"{detail.Trainer.TrainerName} Found");
        await Task.Delay(10000, token);
        var tradepartnersav = new SAV7b();
        var tradepartnersav2 = new SAV7b();
        var tpsarray = await SwitchConnection.ReadBytesAsync(TradePartnerData, 0x168, token);
        tpsarray.CopyTo(tradepartnersav.Blocks.Status.Data);
        var tpsarray2 = await SwitchConnection.ReadBytesAsync(TradePartnerData2, 0x168, token);
        tpsarray2.CopyTo(tradepartnersav2.Blocks.Status.Data);
        if (tradepartnersav.OT != sav.OT)
        {
            Log($"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID},Game: {tradepartnersav.Version}");
            detail.SendNotification(this, $"Found Link Trade Parter: {tradepartnersav.OT}, TID: {tradepartnersav.DisplayTID}, SID: {tradepartnersav.DisplaySID}, Game: {tradepartnersav.Version}");
        }
        if (tradepartnersav2.OT != sav.OT)
        {
            Log($"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}");
            detail.SendNotification(this, $"Found Link Trade Parter: {tradepartnersav2.OT}, TID: {tradepartnersav2.DisplayTID}, SID: {tradepartnersav2.DisplaySID}, Game: {tradepartnersav.Version}");
        }
        foreach (var t in clonelist)
        {
            for (int q = 0; q < clonelist.IndexOf(t); q++)
            {
                await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token);
                await SetStick(SwitchStick.RIGHT, 0, 0, 1000, token).ConfigureAwait(false);
            }
            while (BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == Boxscreen)
            {
                await Click(A, 1000, token);
            }
            detail.SendNotification(this, $"Sending {(Species)t.Species}. You have 15 seconds to select your trade pokemon");
            Log("Waiting on Trade Ccreen");

            await Task.Delay(10_000, token).ConfigureAwait(false);
            detail.SendNotification(this, "You have 5 seconds left to get to the trade screen to not break the trade");
            await Task.Delay(5_000, token);
            var tradeResult = await ConfirmAndStartTrading(detail, clonelist.IndexOf(t), token);
            if (tradeResult != PokeTradeResult.Success)
            {
                if (tradeResult == PokeTradeResult.TrainerLeft)
                    Log("Trade canceled because trainer left the trade.");
                await ExitTrade(false, token).ConfigureAwait(false);
                return tradeResult;
            }

            if (token.IsCancellationRequested)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return PokeTradeResult.RoutineCancel;
            }
            await Task.Delay(30_000, token);
        }
        await ExitTrade(false, token);
        return PokeTradeResult.Success;
    }

    private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PB7> detail, CancellationToken token)
    {
        detail.SendNotification(this, "Highlight the Pokemon in your box, you have 30 seconds");
        var offereddata = await SwitchConnection.ReadBytesAsync(OfferedPokemon, 0x104, token);
        var offeredpbm = new PB7(offereddata);

        detail.SendNotification(this, offeredpbm, "Here is the Pokemon you showed me");

        var quicktime = new Stopwatch();
        quicktime.Restart();
        while (quicktime.ElapsedMilliseconds <= 30_000)
        {
            var newoffereddata = await SwitchConnection.ReadBytesAsync(OfferedPokemon, 0x104, token);
            var newofferedpbm = new PB7(newoffereddata);
            if (SearchUtil.HashByDetails(offeredpbm) != SearchUtil.HashByDetails(newofferedpbm))
            {
                detail.SendNotification(this, newofferedpbm, "Here is the Pokemon you showed me");
                offeredpbm = newofferedpbm;
            }

        }
        Hub.Config.Counts.AddCompletedDumps();
        detail.SendNotification(this, "Time is up!");
        return PokeTradeResult.Success;
    }

    private async Task EnterLinkCodeLG(PokeTradeDetail<PB7> poke, CancellationToken token)
    {
        Hub.Config.Stream.StartEnterCode(this);
        foreach (PictoCodes pc in poke.LGPETradeCode)
        {
            if ((int)pc > 4)
            {
                await SetStick(SwitchStick.RIGHT, 0, -30000, 0, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
            }
            if ((int)pc <= 4)
            {
                for (int i = (int)pc; i > 0; i--)
                {
                    await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    await Task.Delay(500, token).ConfigureAwait(false);
                }
            }
            else
            {
                for (int i = (int)pc - 5; i > 0; i--)
                {
                    await SetStick(SwitchStick.RIGHT, 30000, 0, 0, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    await Task.Delay(500, token).ConfigureAwait(false);
                }
            }
            await Click(A, 200, token).ConfigureAwait(false);
            await Task.Delay(500, token).ConfigureAwait(false);
            if ((int)pc <= 4)
            {
                for (int i = (int)pc; i > 0; i--)
                {
                    await SetStick(SwitchStick.RIGHT, -30000, 0, 0, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    await Task.Delay(500, token).ConfigureAwait(false);
                }
            }
            else
            {
                for (int i = (int)pc - 5; i > 0; i--)
                {
                    await SetStick(SwitchStick.RIGHT, -30000, 0, 0, token).ConfigureAwait(false);
                    await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
                    await Task.Delay(500, token).ConfigureAwait(false);
                }
            }

            if ((int)pc > 4)
            {
                await SetStick(SwitchStick.RIGHT, 0, 30000, 0, token).ConfigureAwait(false);
                await SetStick(SwitchStick.RIGHT, 0, 0, 0, token).ConfigureAwait(false);
            }
        }
    }

    private void WaitAtBarrierIfApplicable(CancellationToken token)
    {
        if (!ShouldWaitAtBarrier)
            return;
        var opt = Hub.Config.Distribution.SynchronizeBots;
        if (opt == BotSyncOption.NoSync)
            return;

        var timeoutAfter = Hub.Config.Distribution.SynchronizeTimeout;
        if (FailedBarrier == 1) // failed last iteration
            timeoutAfter *= 2; // try to re-sync in the event things are too slow.

        var result = Hub.BotSync.Barrier.SignalAndWait(TimeSpan.FromSeconds(timeoutAfter), token);

        if (result)
        {
            FailedBarrier = 0;
            return;
        }

        FailedBarrier++;
        Log($"Barrier sync timed out after {timeoutAfter} seconds. Continuing.");
    }

    private void UpdateBarrier(bool shouldWait)
    {
        if (ShouldWaitAtBarrier == shouldWait)
            return; // no change required

        ShouldWaitAtBarrier = shouldWait;
        if (shouldWait)
        {
            Hub.BotSync.Barrier.AddParticipant();
            Log($"Joined the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
        }
        else
        {
            Hub.BotSync.Barrier.RemoveParticipant();
            Log($"Left the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
        }
    }

    private static ulong GetFakeNID(string trainerName, uint trainerID)
    {
        var nameHash = trainerName.GetHashCode();
        return ((ulong)trainerID << 32) | (uint)nameHash;
    }

    private async Task ExitTrade(bool unexpected, CancellationToken token)
    {
        if (unexpected)
            Log("Unexpected behavior, recovering position.");
        int ctr = 120_000;
        while (!await IsOnOverworldStandard(token))
        {
            if (ctr < 0)
            {
                await RestartGameLGPE(Hub.Config, token).ConfigureAwait(false);
                return;
            }

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await IsOnOverworldStandard(token))
                return;


            await Click(BitConverter.ToUInt16(await SwitchConnection.ReadBytesMainAsync(ScreenOff, 2, token), 0) == Boxscreen ? A : B, 1_000, token).ConfigureAwait(false);
            if (await IsOnOverworldStandard(token))
                return;

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await IsOnOverworldStandard(token))
                return;

            ctr -= 3_000;
        }

    }

    private static readonly HashSet<int> BannedPokemonSpecies = [64, 67, 78, 93];

    private PB7 UpdateToTradePartnerOT(PokeTradeDetail<PB7> poke)
    {
        var cln = poke.TradeData;
        var trainer = LGPEOTInfo.TryGetPreviousTrainerID(poke.Trainer.ID);
        if (trainer != null)
        {
            cln.TrainerTID7 = trainer.TID;
            cln.TrainerSID7 = trainer.SID;
            cln.OriginalTrainerName = trainer.OT;
            cln.OriginalTrainerTrash.Clear();
            cln.OriginalTrainerName = trainer.OT;

            if (poke.TradeData.IsShiny)
                cln.SetShiny();

            cln.RefreshChecksum();
            cln.CurrentHandler = 0;
            cln.ResetCP();
            var la = new LegalityAnalysis(cln);
            if (la.Valid)
            {
                Log($"Pokemon passed legality check using trade partner Info");
                Log($"New Offered Pokemon: {(Species)cln.Species}, TName: {cln.OriginalTrainerName}, TID: {cln.DisplayTID}, SID: {cln.DisplaySID}");
                return cln;
            }
            else
            {
                Log($"Pokemon did not pass legality check. Trade Partner Info could not be applied.");
                return poke.TradeData;
            }
        }
        else
        {
            Log("Trade partner information not yet stored. Trade Partner Info could not be applied");
            return poke.TradeData;
        }
    }

    public static Task CreateLGLinkCodeSpriteEmbed(List<PictoCodes> lgcode)
    {
        int codecount = 0;
        List<Image> spritearray = [];
        foreach (PictoCodes cd in lgcode)
        {
            var showdown = new ShowdownSet(cd.ToString());
            var sav = SaveUtil.GetBlankSAV(EntityContext.Gen7b, "pip");
            var res = sav.GetLegalFromSet(showdown);
            PKM pk = res.Created;
            Image png = pk.Sprite();
            var destRect = new Rectangle(-40, -65, 137, 130);
            var destImage = new Bitmap(137, 130);

            destImage.SetResolution(png.HorizontalResolution, png.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(png, destRect, 0, 0, png.Width, png.Height, GraphicsUnit.Pixel);

            }
            png = destImage;
            spritearray.Add(png);
            codecount++;
        }
        int outputImageWidth = spritearray[0].Width + 20;

        int outputImageHeight = spritearray[0].Height - 65;

        Bitmap outputImage = new(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(outputImage))
        {
            graphics.DrawImage(spritearray[0], new Rectangle(0, 0, spritearray[0].Width, spritearray[0].Height),
                new Rectangle(new Point(), spritearray[0].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[1], new Rectangle(50, 0, spritearray[1].Width, spritearray[1].Height),
                new Rectangle(new Point(), spritearray[1].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[2], new Rectangle(100, 0, spritearray[2].Width, spritearray[2].Height),
                new Rectangle(new Point(), spritearray[2].Size), GraphicsUnit.Pixel);
        }
        Image finalembedpic = outputImage;
        var filename = $"{Directory.GetCurrentDirectory()}//DistributionCode.png";
        finalembedpic.Save(filename);
        return Task.CompletedTask;
    }
}