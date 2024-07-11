namespace SysBot.Pokemon;

/// <summary>
/// Sword &amp; Shield RAM offsets
/// </summary>
public class PokeDataOffsetsSWSH
{
    public const string SWSHGameVersion = "1.3.2";
    public const string SwordID = "0100ABF008968000";
    public const string ShieldID = "01008DB008C2C000";

    public const uint BoxStartOffset = 0x45075880;
    public const uint CurrentBoxOffset = 0x450C680E;
    public const uint TrainerDataOffset = 0x45068F18;
    public const uint SoftBanUnixTimespanOffset = 0x450C89E8;
    public const uint IsConnectedOffset = 0x30c7cca8;
    public const uint TextSpeedOffset = 0x450690A0;
    public const uint ItemTreasureAddress = 0x45068970;
    public const uint LastUsedBallOffset = 0x4C428C80;
    public const uint PokeBallOffset = 0x45067B88; // 0x74 size
    public const uint DenOffset = 0x450C8A70;
    public const uint DexRecMon = 0x45072B18;
    public const uint DexRecMonGender = 0x45072B20;
    public const uint DexRecLocation = 0x45072B98;
    public const uint BerryPouchOffset = 0x45067C50;// 212 bytes
    public const uint IngredientPouchOffset = 0x45068B00;// 100 bytes

    // Link Trade Offsets
    public const uint LinkTradePartnerPokemonOffset = 0xAF286078;
    public const uint LinkTradePartnerNameOffset = 0xAF28384C;
    public const uint LinkTradePartnerTIDSIDOffset = LinkTradePartnerNameOffset - 0x8;
    public const uint LinkTradePartnerNIDOffset = 0xAF2846B0;
    public const uint LinkTradeSearchingOffset = 0x2F76C3C8;
    public const int BoxFormatSlotSize = 0x158;
    public const int TrainerDataLength = 0x110;

    // Surprise Trade Offsets
    public const uint SurpriseTradePartnerPokemonOffset = 0x450675a0;
    public const uint SurpriseTradePartnerNameOffset = 0x45067708;
    public const uint SurpriseTradePartnerTIDSIDOffset = SurpriseTradePartnerNameOffset - 0x8;

    public const uint SurpriseTradeSearchOffset = 0x45067704;
    public const uint SurpriseTradeSearch_Empty = 0x00000000;
    public const uint SurpriseTradeSearch_Searching = 0x01000000;
    public const uint SurpriseTradeSearch_Found = 0x0200012C;

    public const uint SurpriseTradeLockSlot = 0x450676fc;
    public const uint SurpriseTradeLockBox = 0x450676f8;

    #region ScreenDetection
    // Stable overworld detection. Value is 1 on overworld and 0 otherwise.
    public IReadOnlyList<long> OverworldPointer { get; } = new long[] { 0x2636678, 0xC0, 0x80 };

    // For detecting when we're on the in-battle menu, so we can flee.
    public const uint BattleMenuOffset = 0x6B578EDC;

    // Original screen detection offset.
    public const uint CurrentScreenOffset = 0x6B30FA00;
    // Used for checking if we're in a box. It can be either value for different users.
    public const uint CurrentScreen_Box1 = 0xFF00D59B;
    public const uint CurrentScreen_Box2 = 0xFF000000;
    // Value when user is softbanned.
    public const uint CurrentScreen_Softban = 0xFF000000;
    #endregion

    public static uint GetTrainerNameOffset(TradeMethod tradeMethod) => tradeMethod switch
    {
        TradeMethod.LinkTrade => LinkTradePartnerNameOffset,
        TradeMethod.SurpriseTrade => SurpriseTradePartnerNameOffset,
        _ => throw new ArgumentException("Trainer name offset is not available for this trade method.", nameof(tradeMethod)),
    };

    public static uint GetTrainerTIDSIDOffset(TradeMethod tradeMethod) => tradeMethod switch
    {
        TradeMethod.LinkTrade => LinkTradePartnerTIDSIDOffset,
        TradeMethod.SurpriseTrade => SurpriseTradePartnerTIDSIDOffset,
        _ => throw new ArgumentException("Trainer TID/SID offset is not available for this trade method.", nameof(tradeMethod)),
    };
}