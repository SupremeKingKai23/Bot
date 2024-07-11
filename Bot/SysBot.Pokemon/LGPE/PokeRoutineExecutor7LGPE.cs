using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsLGPE;

namespace SysBot.Pokemon;

public abstract class PokeRoutineExecutor7LGPE(PokeBotState cfg) : PokeRoutineExecutor<PB7>(cfg)
{
    public ulong BoxStart = 0x533675B0;
    public readonly int SlotSize = 260;
    public int SlotCount = 25;
    public int GapSize = 380;
    public ulong GetBoxOffset(int box) => BoxStart + (ulong)((SlotSize + GapSize) * SlotCount * box);
    public ulong GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (ulong)((SlotSize + GapSize) * slot);
    public override async Task<PB7> ReadPokemon(ulong offset, CancellationToken token) => await ReadPokemon(offset, BoxFormatSlotSize, token).ConfigureAwait(false);

    public override async Task<PB7> ReadPokemon(ulong offset, int size, CancellationToken token)
    {
        var data = await Connection.ReadBytesAsync((uint)offset, size, token).ConfigureAwait(false);
        return new PB7(data);
    }

    public async Task WriteBoxPokemon(PB7 pk, int box, int slot, CancellationToken token)
    {
        var slotofs = GetSlotOffset(box, slot);
        var StoredLength = SlotSize - 0x1c;
        await Connection.WriteBytesAsync(pk.EncryptedPartyData.AsSpan(0, StoredLength).ToArray(), (uint)slotofs, token);
        await Connection.WriteBytesAsync(pk.EncryptedPartyData.AsSpan(StoredLength).ToArray(), (uint)(slotofs + (ulong)StoredLength + 0x70), token);
    }

    public override async Task<PB7> ReadBoxPokemon(int box, int slot, CancellationToken token)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        throw new NotImplementedException();
    }

    public override Task<PB7> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task<PB7?> ReadUntilPresent(uint offset, int waitms, int waitInterval, CancellationToken token, int size = BoxFormatSlotSize)
    {
        int msWaited = 0;
        while (msWaited < waitms)
        {
            var pk = await ReadPokemon(offset, size, token).ConfigureAwait(false);
            if (pk.Species != 0 && pk.ChecksumValid)
                return pk;
            await Task.Delay(waitInterval, token).ConfigureAwait(false);
            msWaited += waitInterval;
        }
        return null;
    }

    public async Task<SAV7b> IdentifyTrainer(CancellationToken token)
    {
        // Check title so we can warn if mode is incorrect.
        string title = await SwitchConnection.GetTitleID(token).ConfigureAwait(false);
        if (title != LetsGoEeveeID && title != LetsGoPikachuID)
            throw new Exception($"{title} is not a valid Pokémon: Let's Go title. Is your mode correct?");

        var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);
        InitSaveData(sav);

        if (!IsValidTrainerData())
            throw new Exception("Trainer data is not valid. Refer to the SysBot.NET wiki for bad or no trainer data.");
        if (await GetTextSpeed(token).ConfigureAwait(false) < TextSpeedOption.Fast)
            throw new Exception("Text speed should be set to FAST. Fix this for correct operation.");

        return sav;
    }

    public async Task<SAV7b> GetFakeTrainerSAV(CancellationToken token)
    {
        var sav = new SAV7b();
        var info = sav.Blocks.Status;
        var read = await Connection.ReadBytesAsync(TrainerDataOffset, TrainerDataLength, token).ConfigureAwait(false);
        read.CopyTo(info.Data);
        return sav;
    }

    public async Task InitializeHardware(IBotStateSettings settings, CancellationToken token)
    {
        Log("Detaching on startup.");
        await DetachController(token).ConfigureAwait(false);
        if (settings.ScreenOff)
        {
            Log("Turning off screen.");
            await SetScreen(ScreenState.Off, token).ConfigureAwait(false);
     }
        await SetController(ControllerType.JoyRight1, token);
    }

    public async Task CleanExit(CancellationToken token)
    {
        Log("Turning on screen.");
        await SetScreen(ScreenState.On, token).ConfigureAwait(false);
        Log("Detaching controllers on routine exit.");
        await DetachController(token).ConfigureAwait(false);
    }

    public async Task CloseGame(PokeTradeHubConfig config, CancellationToken token)
    {
        var timing = config.Timings;
        await Click(HOME, 2_000 + timing.ExtraTimeReturnHome, token).ConfigureAwait(false);
        await Click(X, 1_000, token).ConfigureAwait(false);
        await Click(A, 5_000 + timing.ExtraTimeCloseGame, token).ConfigureAwait(false);
        Log("Closed out of the game!");
    }

    public async Task StartGame(PokeTradeHubConfig config, CancellationToken token)
    {
        var timing = config.Timings;
        // Open game.
        await Click(A, 1_000 + timing.ExtraTimeLoadProfile, token).ConfigureAwait(false);

        // Menus here can go in the order: Update Prompt -> Profile -> DLC check -> Unable to use DLC.
        //  The user can optionally turn on the setting if they know of a breaking system update incoming.
        if (timing.AvoidSystemUpdate)
        {
            await Click(DUP, 0_600, token).ConfigureAwait(false);
            await Click(A, 1_000 + timing.ExtraTimeLoadProfile, token).ConfigureAwait(false);
        }

        await Click(A, 1_000, token).ConfigureAwait(false);

        Log("Restarting the game!");
        await Task.Delay(4_000 + timing.ExtraTimeLoadGame, token).ConfigureAwait(false);
        await DetachController(token).ConfigureAwait(false);

        while (!await IsOnOverworldStandard(token).ConfigureAwait(false))
            await Click(A, 1_000, token).ConfigureAwait(false);

        Log("Back in the overworld!");
    }

    public async Task<bool> IsOnOverworldStandard(CancellationToken token)
    {
        var data = await Connection.ReadBytesAsync(LGPEStandardOverworldOffset, 1, token).ConfigureAwait(false);
        return data[0] == 1;
    }

    public async Task<TextSpeedOption> GetTextSpeed(CancellationToken token)
    {
        var data = await Connection.ReadBytesAsync(TextSpeedOffset, 1, token).ConfigureAwait(false);
        return (TextSpeedOption)(data[0] & 3);
    }

    public async Task<bool> LGIsinwaitingScreen(CancellationToken token) => BitConverter.ToUInt32(await SwitchConnection.ReadBytesMainAsync(waitingscreen, 4, token).ConfigureAwait(false), 0) == 0;

    public async Task RestartGameLGPE(PokeTradeHubConfig config, CancellationToken token)
    {
        await CloseGame(config, token);
        await StartGame(config, token);
    }
}