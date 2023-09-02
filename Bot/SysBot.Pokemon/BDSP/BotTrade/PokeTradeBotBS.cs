﻿using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System.Net.Sockets;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.BasePokeDataOffsetsBS;
using static SysBot.Pokemon.SpecialRequests;

namespace SysBot.Pokemon
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class PokeTradeBotBS : PokeRoutineExecutor8BS, ICountBot
    {
        private readonly PokeTradeHub<PB8> Hub;
        private readonly TradeSettings TradeSettings;
        private readonly TradeAbuseSettings AbuseSettings;

        public ICountSettings Counts => TradeSettings;

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        private readonly IDumper DumpSetting;

        /// <summary>
        /// Synchronized start for multiple bots.
        /// </summary>
        public bool ShouldWaitAtBarrier { get; private set; }

        /// <summary>
        /// Tracks failed synchronized starts to attempt to re-sync.
        /// </summary>
        public int FailedBarrier { get; private set; }

        public PokeTradeBotBS(PokeTradeHub<PB8> hub, PokeBotState cfg) : base(cfg)
        {
            Hub = hub;
            TradeSettings = hub.Config.Trade;
            AbuseSettings = hub.Config.TradeAbuse;
            DumpSetting = hub.Config.Folder;
            lastOffered = new byte[8];
        }

        // Cached offsets that stay the same per session.
        private ulong BoxStartOffset;
        private ulong UnionGamingOffset;
        private ulong UnionTalkingOffset;
        private ulong SoftBanOffset;
        private ulong LinkTradePokemonOffset;

        // Track the last Pokémon we were offered since it persists between trades.
        private byte[] lastOffered;

        public override async Task MainLoop(CancellationToken token)
        {
            try
            {
                await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

                Log("Identifying trainer data of the host console.");
                var sav = await IdentifyTrainer(token).ConfigureAwait(false);
                RecentTrainerCache.SetRecentTrainer(sav);

                await RestartGameIfCantLeaveUnionRoom(token).ConfigureAwait(false);
                await InitializeSessionOffsets(token).ConfigureAwait(false);

                Log($"Starting main {nameof(PokeTradeBotBS)} loop.");
                await InnerLoop(sav, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }

            Log($"Ending {nameof(PokeTradeBotBS)} loop.");
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            UpdateBarrier(false);
            await CleanExit(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task InnerLoop(SAV8BS sav, CancellationToken token)
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
                    if (e.StackTrace != null)
                        Connection.LogError(e.StackTrace);
                    var attempts = Hub.Config.Timings.ReconnectAttempts;
                    var delay = Hub.Config.Timings.ExtraReconnectDelay;
                    var protocol = Config.Connection.Protocol;
                    if (!await TryReconnect(attempts, delay, protocol, token).ConfigureAwait(false))
                        return;
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

        private async Task DoTrades(SAV8BS sav, CancellationToken token)
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
                if (detail.Type != PokeTradeType.Random || !Hub.Config.Distribution.RemainInUnionRoomBDSP)
                    await RestartGameIfCantLeaveUnionRoom(token).ConfigureAwait(false);
                string tradetype = $" ({detail.Type})";
                Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
                await Task.Delay(500, token).ConfigureAwait(false);
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

        protected virtual (PokeTradeDetail<PB8>? detail, uint priority) GetTradeData(PokeRoutineType type)
        {
            if (Hub.Queues.TryDequeue(type, out var detail, out var priority))
                return (detail, priority);
            if (Hub.Queues.TryDequeueLedy(out detail))
                return (detail, PokeTradePriorities.TierFree);
            return (null, PokeTradePriorities.TierFree);
        }

        private async Task PerformTrade(SAV8BS sav, PokeTradeDetail<PB8> detail, PokeRoutineType type, uint priority, CancellationToken token)
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

        private void HandleAbortedTrade(PokeTradeDetail<PB8> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
        {
            detail.IsProcessing = false;
            if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
            {
                detail.IsRetry = true;
                Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
                detail.SendNotification(this, "Oops! Something happened. I'll requeue you for another attempt.");
            }
            else
            {
                detail.SendNotification(this, $"Oops! Something happened. Canceling the trade: {result}.");
                detail.TradeCanceled(this, result);
            }
        }

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8BS sav, PokeTradeDetail<PB8> poke, CancellationToken token)
        {
            if (Hub.Config.Stream.CreateAssets)
            {
                if (poke.Type == PokeTradeType.Random)
                    SetText(sav, $"Trade code: {poke.Code:0000 0000}\r\nSending: {(Species)poke.TradeData.Species}{(poke.TradeData.IsEgg ? " (egg)" : string.Empty)}");
                else
                    SetText(sav, "Running a\nSpecific trade.");
            }

            // Update Barrier Settings
            UpdateBarrier(poke.IsSynchronized);
            poke.TradeInitialize(this);
            Hub.Config.Stream.EndEnterCode(this);

            var distroRemainInRoom = poke.Type == PokeTradeType.Random && Hub.Config.Distribution.RemainInUnionRoomBDSP;

            // If we weren't supposed to remain and started out in the Union Room, ensure we're out of the box.
            if (!distroRemainInRoom && await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            {
                if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                    return PokeTradeResult.RecoverReturnOverworld;
            }

            if (await CheckIfSoftBanned(SoftBanOffset, token).ConfigureAwait(false))
                await UnSoftBan(token).ConfigureAwait(false);

            var toSend = poke.TradeData;
            if (toSend.Species != 0)
                await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);

            // Enter Union Room. Shouldn't do anything if we're already there.
            if (!await EnterUnionRoomWithCode(poke.Type, poke.Code, token).ConfigureAwait(false))
            {
                // We don't know how far we made it in, so restart the game to be safe.
                await RestartGameBDSP(token).ConfigureAwait(false);
                return PokeTradeResult.RecoverEnterUnionRoom;
            }

            await RequestUnionRoomTrade(token).ConfigureAwait(false);
            poke.TradeSearching(this);
            var waitPartner = Hub.Config.Trade.TradeWaitTime;

            // Keep pressing A until we detect someone talking to us.
            while (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false) && waitPartner > 0)
            {
                for (int i = 0; i < 2; ++i)
                    await Click(A, 0_450, token).ConfigureAwait(false);

                if (--waitPartner <= 0)
                    return PokeTradeResult.NoTrainerFound;
            }
            Log("Found a user talking to us!");

            // Keep pressing A until TargetTranerParam (sic) is loaded (when we hit the box).
            while (!await IsPartnerParamLoaded(token).ConfigureAwait(false) && waitPartner > 0)
            {
                for (int i = 0; i < 2; ++i)
                    await Click(A, 0_450, token).ConfigureAwait(false);

                // Can be false if they talked and quit.
                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    break;
                if (--waitPartner <= 0)
                    return PokeTradeResult.TrainerTooSlow;
            }
            Log("Entering the box...");

            // Still going through dialog and box opening.
            await Task.Delay(3_000, token).ConfigureAwait(false);

            // Can happen if they quit out of talking to us.
            if (!await IsPartnerParamLoaded(token).ConfigureAwait(false))
                return PokeTradeResult.TrainerTooSlow;

            var tradePartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
            var trainerNID = GetFakeNID(tradePartner.TrainerName, tradePartner.IDHash);
            RecordUtil<PokeTradeBotSWSH>.Record($"Initiating\t{trainerNID:X16}\t{tradePartner.TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
            Log($"Found Link Trade partner: {tradePartner.TrainerName}-{tradePartner.TID7} (ID: {trainerNID}");

            var partnerCheck = await CheckPartnerReputation(this, poke, trainerNID, tradePartner.TrainerName, AbuseSettings, token);
            if (partnerCheck != PokeTradeResult.Success)
            {
                // Try to get out of the box.
                if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                    return PokeTradeResult.RecoverReturnOverworld;

                // Leave the Union room if we chose not to stay.
                if (!distroRemainInRoom)
                {
                    Log("Trying to get out of the Union Room.");
                    if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
                        return PokeTradeResult.RecoverReturnOverworld;
                }
                return PokeTradeResult.SuspiciousActivity;
            }

            await Task.Delay(2_000 + Hub.Config.Timings.ExtraTimeOpenBox, token).ConfigureAwait(false);

            poke.SendNotification(this, $"Found Link Trade partner: {tradePartner.TrainerName}. Waiting for a Pokémon...");

            // Requires at least one trade for this pointer to make sense, so cache it here.
            LinkTradePokemonOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);

            if (poke.Type == PokeTradeType.Dump)
                return await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);

            // Wait for user input... Needs to be different from the previously offered Pokémon.
            var tradeOffered = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 25_000, 1_000, false, true, token).ConfigureAwait(false);
            if (!tradeOffered)
                return PokeTradeResult.TrainerTooSlow;

            // If we detected a change, they offered something.
            var offered = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);

            if (Hub.Config.Legality.UseTradePartnerInfo && poke.Type != PokeTradeType.SpecialRequest)
            {
                Log($"Changing OT to Partner OT");
                await SetBoxPkmWithSwappedIDDetailsBDSP(toSend, offered, sav, tradePartner, token);
                await Click(R, 1_500, token).ConfigureAwait(false);
                await Click(L, 1_500, token).ConfigureAwait(false);
                await Task.Delay(1_500, token).ConfigureAwait(false);
            }

            // Confirm Box 1 Slot 1
            if (poke.Type == PokeTradeType.Specific || poke.Type == PokeTradeType.SupportTrade)
            {
                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }

            if (offered.Species == 0 || !offered.ChecksumValid)
                return PokeTradeResult.TrainerTooSlow;
            lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);

            SpecialTradeType itemReq = SpecialTradeType.None;
            if (poke.Type == PokeTradeType.SpecialRequest)
                itemReq = CheckItemRequest(ref offered, this, poke, tradePartner.TrainerName, (uint)tradePartner.TID7, (uint)tradePartner.SID7, sav, Hub.Config.Folder.SpecialRequestWCFolder);
            if (itemReq == SpecialTradeType.FailReturn)
                return PokeTradeResult.IllegalTrade;

            if (poke.Type == PokeTradeType.SpecialRequest && itemReq == SpecialTradeType.None)
            {
                // Immediately exit, we aren't trading anything.
                poke.SendNotification(this, "No held item or valid request");
                await ExitBoxToUnionRoom(token).ConfigureAwait(false);
            }


            var offset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);
            var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(offset, 4, token).ConfigureAwait(false);
            PokeTradeResult update;
            var trainer = new PartnerDataHolder(trainerNID, tradePartner.TrainerName, tradePartner.TID7.ToString());
            (toSend, update) = await GetEntityToSend(sav, poke, offered, oldEC, toSend, trainer, poke.Type == PokeTradeType.SpecialRequest ? itemReq : null, token).ConfigureAwait(false);
            if (update != PokeTradeResult.Success)
            {
                if (itemReq != SpecialTradeType.None)
                {
                    poke.SendNotification(this, "Your request isn't legal. Please try a different Pokémon or request.");
                }

                return update;
            }

            var clone = offered.Clone();
            if (itemReq == SpecialTradeType.WonderCard)
            {
                poke.SendNotification(this, $"{(Species)clone.Species} Distribution success!");
                Log($"{(Species)clone.Species} Distribution success!");
            }
            else if (itemReq != SpecialTradeType.None && itemReq != SpecialTradeType.Shinify)
            {
                poke.SendNotification(this, "Special request successful!");
                Log($"Successfully modified their {(Species)clone.Species}!");
            }
            else if (itemReq == SpecialTradeType.Shinify)
            {
                poke.SendNotification(this, "Shinify success!");
                Log($"Shinified their {(Species)clone.Species}!");
            }

            var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
            if (tradeResult != PokeTradeResult.Success)
                return tradeResult;

            if (token.IsCancellationRequested)
                return PokeTradeResult.RoutineCancel;

            // Trade was Successful!
            var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
            // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
            if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
            {
                Log("User did not complete the trade.");
                return PokeTradeResult.TrainerTooSlow;
            }

            // As long as we got rid of our inject in b1s1, assume the trade went through.
            Log("User completed the trade.");
            poke.TradeFinished(this, received);

            // Only log if we completed the trade.
            UpdateCountsAndExport(poke, received, toSend);

            // Log for Trade Abuse tracking.
            LogSuccessfulTrades(poke, trainerNID, tradePartner.TrainerName);

            // Try to get out of the box.
            if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                return PokeTradeResult.RecoverReturnOverworld;

            // Leave the Union room if we chose not to stay.
            if (!distroRemainInRoom)
            {
                Log("Trying to get out of the Union Room.");
                if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
                    return PokeTradeResult.RecoverReturnOverworld;
            }

            // Sometimes they offered another mon, so store that immediately upon leaving Union Room.
            lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);

            return PokeTradeResult.Success;
        }

        private static ulong GetFakeNID(string trainerName, uint trainerID)
        {
            var nameHash = trainerName.GetHashCode();
            return ((ulong)trainerID << 32) | (uint)nameHash;
        }

        private void UpdateCountsAndExport(PokeTradeDetail<PB8> poke, PB8 received, PB8 toSend)
        {
            var counts = TradeSettings;
            if (poke.Type == PokeTradeType.Random)
                counts.AddCompletedDistribution();
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
                if (poke.Type is PokeTradeType.Specific or PokeTradeType.FixOT or PokeTradeType.SupportTrade or PokeTradeType.SpecialRequest)
                    DumpPokemon(DumpSetting.DumpFolder, "traded", toSend); // sent to partner
            }
        }

        private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PB8> detail, CancellationToken token)
        {
            // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
            var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);

            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < Hub.Config.Trade.MaxTradeConfirmTime; i++)
            {
                if (await IsUserBeingShifty(detail, token).ConfigureAwait(false))
                    return PokeTradeResult.SuspiciousActivity;
                // We're no longer talking, so they probably quit on us.
                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    return PokeTradeResult.TrainerTooSlow;
                await Click(A, 1_000, token).ConfigureAwait(false);

                // EC is detectable at the start of the animation.
                var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);
                if (!newEC.SequenceEqual(oldEC))
                {
                    await Task.Delay(25_000, token).ConfigureAwait(false);
                    return PokeTradeResult.Success;
                }
            }

            // If we don't detect a B1S1 change, the trade didn't go through in that time.
            return PokeTradeResult.TrainerTooSlow;
        }

        private async Task<bool> EnterUnionRoomWithCode(PokeTradeType tradeType, int tradeCode, CancellationToken token)
        {
            // Already in Union Room.
            if (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
                return true;

            // Open y-comm and select global room
            await Click(Y, 1_000 + Hub.Config.Timings.ExtraTimeOpenYMenu, token).ConfigureAwait(false);
            await Click(DRIGHT, 0_400, token).ConfigureAwait(false);

            // French has one less menu
            if (GameLang is not LanguageID.French)
            {
                await Click(A, 0_050, token).ConfigureAwait(false);
                await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
            }

            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);

            // Japanese has one extra menu
            if (GameLang is LanguageID.Japanese)
            {
                await Click(A, 0_050, token).ConfigureAwait(false);
                await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
            }

            await Click(A, 1_000, token).ConfigureAwait(false); // Would you like to enter? Screen

            Log("Selecting Link Code room.");
            // Link code selection index
            await Click(DDOWN, 0_200, token).ConfigureAwait(false);
            await Click(DDOWN, 0_200, token).ConfigureAwait(false);

            Log("Connecting to internet.");
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 2_000, 0, token).ConfigureAwait(false);

            // Extra menus.
            if (GameLang is LanguageID.German or LanguageID.Italian or LanguageID.Korean)
            {
                await Click(A, 0_050, token).ConfigureAwait(false);
                await PressAndHold(A, 0_750, 0, token).ConfigureAwait(false);
            }

            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);

            // Would you like to save your adventure so far?
            await Click(A, 0_500, token).ConfigureAwait(false);
            await Click(A, 0_500, token).ConfigureAwait(false);

            Log("Saving the game.");
            // Agree and save the game.
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 6_500, 0, token).ConfigureAwait(false);

            if (tradeType != PokeTradeType.Random)
                Hub.Config.Stream.StartEnterCode(this);
            Log($"Entering Link Trade code: {tradeCode:0000 0000}...");
            await EnterLinkCode(tradeCode, Hub.Config, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            WaitAtBarrierIfApplicable(token);
            await Click(PLUS, 0_600, token).ConfigureAwait(false);
            Hub.Config.Stream.EndEnterCode(this);
            Log("Entering the Union Room.");

            // Wait until we're past the communication message.
            int tries = 100;
            while (!await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            {
                await Click(A, 0_300, token).ConfigureAwait(false);

                if (--tries < 1)
                    return false;
            }

            await Task.Delay(1_300 + Hub.Config.Timings.ExtraTimeJoinUnionRoom, token).ConfigureAwait(false);

            return true; // We've made it into the room and are ready to request.
        }

        private async Task RequestUnionRoomTrade(CancellationToken token)
        {
            // Y-button trades always put us in a place where we can open the call menu without having to move.
            Log("Attempting to open the Y menu.");
            await Click(Y, 1_000, token).ConfigureAwait(false);
            await Click(A, 0_400, token).ConfigureAwait(false);
            await Click(DDOWN, 0_400, token).ConfigureAwait(false);
            await Click(DDOWN, 0_400, token).ConfigureAwait(false);
            await Click(A, 0_100, token).ConfigureAwait(false);
            Log("Waiting For Trainer.");
        }

        // These don't change per session and we access them frequently, so set these each time we start.
        private async Task InitializeSessionOffsets(CancellationToken token)
        {
            Log("Caching session offsets...");
            BoxStartOffset = await SwitchConnection.PointerAll(Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
            UnionGamingOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkIsGamingPointer, token).ConfigureAwait(false);
            UnionTalkingOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkIsTalkingPointer, token).ConfigureAwait(false);
            SoftBanOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkPenaltyPointer, token).ConfigureAwait(false);
        }

        // todo: future
        protected virtual async Task<bool> IsUserBeingShifty(PokeTradeDetail<PB8> detail, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return false;
        }

        private async Task RestartGameIfCantLeaveUnionRoom(CancellationToken token)
        {
            if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
                await RestartGameBDSP(token).ConfigureAwait(false);
        }

        private async Task RestartGameBDSP(CancellationToken token)
        {
            await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
            await InitializeSessionOffsets(token).ConfigureAwait(false);
        }

        private async Task<bool> EnsureOutsideOfUnionRoom(CancellationToken token)
        {
            if (!await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
                return true;

            if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                return false;
            if (!await ExitUnionRoomToOverworld(token).ConfigureAwait(false))
                return false;
            return true;
        }

        private async Task<bool> ExitBoxToUnionRoom(CancellationToken token)
        {
            if (await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
            {
                Log("Exiting box...");
                int tries = 30;
                while (await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                {
                    await Click(B, 0_500, token).ConfigureAwait(false);
                    if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                        break;
                    await Click(DUP, 0_200, token).ConfigureAwait(false);
                    await Click(A, 0_500, token).ConfigureAwait(false);
                    // Keeps regular quitting a little faster, only need this for trade evolutions + moves.
                    if (tries < 10)
                        await Click(B, 0_500, token).ConfigureAwait(false);
                    await Click(B, 0_500, token).ConfigureAwait(false);
                    tries--;
                    if (tries < 0)
                        return false;
                }
            }
            await Task.Delay(2_000, token).ConfigureAwait(false);
            return true;
        }

        private async Task<bool> ExitUnionRoomToOverworld(CancellationToken token)
        {
            if (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            {
                Log("Exiting Union Room...");
                for (int i = 0; i < 3; ++i)
                    await Click(B, 0_200, token).ConfigureAwait(false);

                await Click(Y, 1_000, token).ConfigureAwait(false);
                await Click(DDOWN, 0_200, token).ConfigureAwait(false);
                for (int i = 0; i < 3; ++i)
                    await Click(A, 0_400, token).ConfigureAwait(false);

                int tries = 10;
                while (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
                {
                    await Task.Delay(0_400, token).ConfigureAwait(false);
                    tries--;
                    if (tries < 0)
                        return false;
                }
                await Task.Delay(3_000 + Hub.Config.Timings.ExtraTimeLeaveUnionRoom, token).ConfigureAwait(false);
            }
            return true;
        }

        private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PB8> detail, CancellationToken token)
        {
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;

            var bctr = 0;
            while (ctr < Hub.Config.Trade.MaxDumpsPerTrade && DateTime.Now - start < time)
            {
                // We're no longer talking, so they probably quit on us.
                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    break;
                if (bctr++ % 3 == 0)
                    await Click(B, 0_100, token).ConfigureAwait(false);

                // Wait for user input... Needs to be different from the previously offered Pokémon.
                var tradeOffered = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 3_000, 1_000, false, true, token).ConfigureAwait(false);
                if (!tradeOffered)
                    continue;

                // If we detected a change, they offered something.
                var pk = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
                var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);
                if (pk.Species < 1 || !pk.ChecksumValid || lastOffered == newEC)
                    continue;
                lastOffered = newEC;

                // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
                if (DumpSetting.Dump)
                {
                    var subfolder = detail.Type.ToString().ToLower();
                    DumpPokemon(DumpSetting.DumpFolder, subfolder, pk); // received
                }

                var la = new LegalityAnalysis(pk);
                var verbose = $"```{la.Report(true)}```";
                Log($"Shown Pokémon is: {(la.Valid ? "Valid" : "Invalid")}.");

                ctr++;
                var msg = Hub.Config.Trade.DumpTradeLegalityCheck ? verbose : $"File {ctr}";

                // Extra information about trainer data for people requesting with their own trainer data.
                var ot = pk.OT_Name;
                var ot_gender = pk.OT_Gender == 0 ? "Male" : "Female";
                var tid = pk.GetDisplayTID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringTID());
                var sid = pk.GetDisplaySID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringSID());
                msg += $"\n**Trainer Data**\n```OT: {ot}\nOTGender: {ot_gender}\nTID: {tid}\nSID: {sid}```";

                // Extra information for shiny eggs, because of people dumping to skip hatching.
                var eggstring = pk.IsEgg ? "Egg " : string.Empty;
                msg += pk.IsShiny ? $"\n**This Pokémon {eggstring}is shiny!**" : string.Empty;
                detail.SendNotification(this, pk, msg);
            }

            Log($"Ended Dump loop after processing {ctr} Pokémon.");
            if (ctr == 0)
                return PokeTradeResult.TrainerTooSlow;

            TradeSettings.AddCompletedDumps();
            detail.Notifier.SendNotification(this, detail, $"Dumped {ctr} Pokémon.");
            detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank pk8
            return PokeTradeResult.Success;
        }

        private async Task<TradePartnerBS> GetTradePartnerInfo(CancellationToken token)
        {
            var id = await SwitchConnection.PointerPeek(4, Offsets.LinkTradePartnerIDPointer, token).ConfigureAwait(false);
            var name = await SwitchConnection.PointerPeek(TradePartnerBS.MaxByteLengthStringObject, Offsets.LinkTradePartnerNamePointer, token).ConfigureAwait(false);
            var traderOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerIDPointer, token).ConfigureAwait(false);
            var idbytes = await SwitchConnection.ReadBytesAbsoluteAsync(traderOffset + 0x04, 4, token).ConfigureAwait(false);
            return new TradePartnerBS(id, idbytes, name);
        }

        protected virtual async Task<(PB8 toSend, PokeTradeResult check)> GetEntityToSend(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, byte[] oldEC, PB8 toSend, PartnerDataHolder partnerID, SpecialTradeType? stt, CancellationToken token)
        {
            return poke.Type switch
            {
                PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
                PokeTradeType.FixOT => await HandleFixOT(sav, poke, offered, partnerID, token).ConfigureAwait(false),
                PokeTradeType.SpecialRequest when stt is not SpecialTradeType.WonderCard => await HandleClone(sav, poke, offered, oldEC, token).ConfigureAwait(false),
                PokeTradeType.SpecialRequest when stt is SpecialTradeType.WonderCard => await JustInject(sav, offered, token).ConfigureAwait(false),
                _ => (toSend, PokeTradeResult.Success),
            };
        }

        private async Task<(PB8 toSend, PokeTradeResult check)> HandleRandomLedy(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, PB8 toSend, PartnerDataHolder partner, CancellationToken token)
        {
            // Allow the trade partner to do a Ledy swap.
            var config = Hub.Config.Distribution;
            var trade = Hub.Ledy.GetLedyTrade(offered, partner.TrainerOnlineID, config.LedySpecies);
            if (trade != null)
            {
                if (trade.Type == LedyResponseType.AbuseDetected)
                {
                    var msg = $"Found {partner.TrainerName} has been detected for abusing Ledy trades.";
                    EchoUtil.Echo(msg);

                    return (toSend, PokeTradeResult.SuspiciousActivity);
                }

                toSend = trade.Receive;
                poke.TradeData = toSend;

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
                await Task.Delay(2_500, token).ConfigureAwait(false);
            }
            else if (config.LedyQuitIfNoMatch)
            {
                return (toSend, PokeTradeResult.TrainerRequestBad);
            }

            for (int i = 0; i < 5; i++)
            {
                await Click(A, 0_500, token).ConfigureAwait(false);
            }

            return (toSend, PokeTradeResult.Success);
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

        /// <summary>
        /// Checks if the barrier needs to get updated to consider this bot.
        /// If it should be considered, it adds it to the barrier if it is not already added.
        /// If it should not be considered, it removes it from the barrier if not already removed.
        /// </summary>
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

        private static void SetText(SAV8BS sav, string text)
        {
            File.WriteAllText($"code{sav.OT}-{sav.DisplayTID}.txt", text);
        }

        private async Task<(PB8 toSend, PokeTradeResult check)> HandleFixOT(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, PartnerDataHolder partner, CancellationToken token)
        {
            if (Hub.Config.Discord.ReturnPKMs)
                poke.SendNotification(this, offered, "Here's what you showed me!");

            var adOT = TradeExtensions<PB8>.HasAdName(offered, out _);
            var laInit = new LegalityAnalysis(offered);
            if (!adOT && laInit.Valid)
            {
                poke.SendNotification(this, "No ad detected in Nickname or OT, and the Pokémon is legal. Exiting trade.");
                return (offered, PokeTradeResult.TrainerRequestBad);
            }

            var clone = offered.Clone();
            if (Hub.Config.Legality.ResetHOMETracker)
                clone.Tracker = 0;

            string shiny = string.Empty;
            if (!TradeExtensions<PB8>.ShinyLockCheck(offered.Species, TradeExtensions<PB8>.FormOutput(offered.Species, offered.Form, out _), $"{(Ball)offered.Ball}"))
                shiny = $"\nShiny: {(offered.ShinyXor == 0 ? "Square" : offered.IsShiny ? "Star" : "No")}";
            else shiny = "\nShiny: No";

            var name = partner.TrainerName;
            var ball = $"\n{(Ball)offered.Ball}";
            var extraInfo = $"OT: {name}{ball}{shiny}";
            var set = ShowdownParsing.GetShowdownText(offered).Split('\n').ToList();
            var shinyRes = set.Find(x => x.Contains("Shiny"));
            if (shinyRes != null)
                set.Remove(shinyRes);
            set.InsertRange(1, extraInfo.Split('\n'));

            if (!laInit.Valid)
            {
                Log($"FixOT request has detected an illegal Pokémon from {name}: {(Species)offered.Species}");
                var report = laInit.Report();
                Log(laInit.Report());
                poke.SendNotification(this, $"**Shown Pokémon is not legal. Attempting to regenerate...**\n\n```{report}```");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);
            }

            if (clone.FatefulEncounter)
            {
                clone.SetDefaultNickname(laInit);
                var info = new SimpleTrainerInfo { Gender = clone.OT_Gender, Language = clone.Language, OT = name, TID16 = clone.TID16, SID16 = clone.SID16, Generation = 8 };
                var mg = EncounterEvent.GetAllEvents().Where(x => x.Species == clone.Species && x.Form == clone.Form && x.IsShiny == clone.IsShiny && x.OT_Name == clone.OT_Name).ToList();
                if (mg.Count > 0)
                    clone = TradeExtensions<PB8>.CherishHandler(mg.First(), info);
                else clone = (PB8)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);
            }
            else clone = (PB8)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);

            clone = (PB8)TradeExtensions<PB8>.TrashBytes(clone, new LegalityAnalysis(clone));
            clone.ResetPartyStats();
            var la = new LegalityAnalysis(clone);
            if (!la.Valid)
            {
                poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I was unable to fix this. Exiting trade.");
                return (clone, PokeTradeResult.IllegalTrade);
            }

            poke.SendNotification(this, $"{(!laInit.Valid ? "**Legalized" : "**Fixed Nickname/OT for")} {(Species)clone.Species}**!");
            Log($"{(!laInit.Valid ? "Legalized" : "Fixed Nickname/OT for")} {(Species)clone.Species}!");

            await SetBoxPokemonAbsolute(BoxStartOffset, clone, token, sav).ConfigureAwait(false);
            poke.SendNotification(this, "Now confirm the trade!");
            await Click(A, 0_800, token).ConfigureAwait(false);
            await Click(A, 6_000, token).ConfigureAwait(false);

            var pk2 = await ReadPokemon(LinkTradePokemonOffset, token).ConfigureAwait(false);
            var comp = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);
            bool changed = pk2 == null || comp != lastOffered || clone.Species != pk2.Species || offered.OT_Name != pk2.OT_Name;
            if (changed)
            {
                Log($"{name} changed the shown Pokémon ({(Species)clone.Species}){(pk2 != null ? $" to {(Species)pk2.Species}" : "")}");
                poke.SendNotification(this, "**Send away the originally shown Pokémon, please!**");

                bool verify = await ReadUntilChanged(LinkTradePokemonOffset, comp, 10_000, 0_200, false, true, token).ConfigureAwait(false);
                if (verify)
                    verify = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 5_000, 0_200, true, true, token).ConfigureAwait(false);
                changed = !verify && (pk2 == null || clone.Species != pk2.Species || offered.OT_Name != pk2.OT_Name);
            }

            // Update the last Pokémon they showed us.
            lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);

            if (changed)
            {
                poke.SendNotification(this, "Pokémon was swapped and not changed back. Exiting trade.");
                Log("Trading partner did not wish to send away their ad-mon.");
                return (offered, PokeTradeResult.TrainerTooSlow);
            }

            await Click(A, 0_500, token).ConfigureAwait(false);
            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);

            return (clone, PokeTradeResult.Success);
        }

        private async Task<(PB8 toSend, PokeTradeResult check)> HandleClone(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, byte[] oldEC, CancellationToken token)
        {
            if (Hub.Config.Discord.ReturnPKMs)
                poke.SendNotification(this, offered, "Here's what you showed me!");

            var la = new LegalityAnalysis(offered);
            if (!la.Valid)
            {
                Log($"Clone request (from {poke.Trainer.TrainerName}) has detected an invalid Pokémon: {(Species)offered.Species}.");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);

                var report = la.Report();
                Log(report);
                poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from cloning this. Exiting trade.");
                poke.SendNotification(this, report);

                return (offered, PokeTradeResult.IllegalTrade);
            }

            // Inject the shown Pokémon.
            var clone = offered.Clone();
            if (Hub.Config.Legality.ResetHOMETracker)
                clone.Tracker = 0;

            poke.SendNotification(this, $"**Cloned your {(Species)clone.Species}!**\nYou must Trade me the Original for this game.");
            Log($"Cloned a {(Species)clone.Species}.");

            await SetBoxPokemonAbsolute(BoxStartOffset, clone, token, sav).ConfigureAwait(false);
            await Click(A, 0_800, token).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);

            return (clone, PokeTradeResult.Success);
        }

        private async Task<(PB8 toSend, PokeTradeResult check)> JustInject(SAV8BS sav, PB8 offered, CancellationToken token)
        {
            await Click(A, 0_800, token).ConfigureAwait(false);
            if (offered.OT_Name == sav.OT)
            {
                var tradePartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
                Log($"Changing OT to Partner OT");
                await SetBoxPkmWithSwappedIDDetailsBDSP(offered, offered, sav, tradePartner, token);
                await Click(R, 1_500, token).ConfigureAwait(false);
                await Click(L, 1_500, token).ConfigureAwait(false);
                await Task.Delay(1_500, token).ConfigureAwait(false);
            }
            else
            {
                await SetBoxPokemonAbsolute(BoxStartOffset, offered, token, sav).ConfigureAwait(false);
            }

            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);

            return (offered, PokeTradeResult.Success);
        }

        private async Task<bool> SetBoxPkmWithSwappedIDDetailsBDSP(PB8 toSend, PB8 offered, SAV8BS sav, TradePartnerBS tradePartner, CancellationToken token)
        {
            // ignore using trade partner info for Ditto
            if (toSend.Species == (ushort)Species.Ditto)
            {
                Log($"Ditto detected. Trade Partner info will not be applied.");
                return false;
            }
            var cln = toSend.Clone();
            cln.OT_Gender = offered.OT_Gender;
            cln.TrainerTID7 = (uint)tradePartner.TID7;
            cln.TrainerSID7 = (uint)tradePartner.SID7;
            cln.Language = offered.Language;
            cln.OT_Name = tradePartner.TrainerName;

            // Handle egg
            if (toSend.IsEgg == true)
            {
                cln.IsNicknamed = true;
                cln.Nickname = offered.Language switch
                {
                    1 => "タマゴ",
                    3 => "Œuf",
                    4 => "Uovo",
                    5 => "Ei",
                    7 => "Huevo",
                    8 => "알",
                    9 or 10 => "蛋",
                    _ => "Egg",
                };
            }

            if (toSend.IsShiny)
                cln.SetShiny();

            cln.RefreshChecksum();
            cln.CurrentHandler = 0;
            var tradeBS = new LegalityAnalysis(cln);
            if (tradeBS.Valid)
            {
                Log($"Pokemon passed legality check using trade partner Info");
                Log($"New Offered Pokemon: {(Species)cln.Species}, TName: {cln.OT_Name}, TID: {cln.DisplayTID}, SID: {cln.DisplaySID}, Language: {cln.Language}, OTGender: {cln.OT_Gender}");
                await SetBoxPokemonAbsolute(BoxStartOffset, cln, token, sav).ConfigureAwait(false);
            }
            else
            {
                Log($"Pokemon did not pass legality check. Trade Partner Info could not be used.");
            }

            return tradeBS.Valid;
        }
    }
}