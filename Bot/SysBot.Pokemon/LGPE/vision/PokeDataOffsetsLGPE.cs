namespace SysBot.Pokemon;

/// <summary>
/// Pokémon Legends: Arceus RAM offsets
/// </summary>
public static class PokeDataOffsetsLGPE
{
    public const string LetsGoPikachuID = "010003F003A34000";
    public const string LetsGoEeveeID = "0100187003A36000";

    public const uint TrainerDataOffset = 0x53321CF0;
    public const uint TextSpeedOffset = 0x53321EDC;
    public const int BoxFormatSlotSize = 0x104;
    public const int TrainerDataLength = 0x168;
    public const uint LGPEStandardOverworldOffset = 0x5E1CE550;  // Can be used for overworld checks in anything but battle.
    public const uint waitingscreen = 0x15363d8;
    public const uint ScreenOff = 0x1610E68;
    public const uint savescreen = 0x7250;
    public const uint savescreen2 = 0x6250;
    public static uint menuscreen = 0xD080;
    public static uint Boxscreen = 0xF080;
    public static uint waitingtotradescreen = 0x0080;
    public const uint TradePartnerData = 0x41A28240;
    public const uint TradePartnerData2 = 0x41A28078;
    public const uint OfferedPokemon = 0x41A22858;
}