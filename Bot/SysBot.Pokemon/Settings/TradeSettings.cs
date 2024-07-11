using PKHeX.Core;
using SysBot.Base;
using System.ComponentModel;

namespace SysBot.Pokemon;

public class TradeSettings : IBotStateSettings
{
    private const string TradeCode = nameof(TradeCode);
    private const string TradeConfig = nameof(TradeConfig);
    private const string Dumping = nameof(Dumping);
    private const string Embeds = nameof(Embeds);
    private const string Misc = nameof(Misc);
    public override string ToString() => "Trade Bot Settings";

    [Category(TradeConfig), Description("Time to wait for a trade partner in seconds.")]
    public int TradeWaitTime { get; set; } = 30;

    [Category(TradeConfig), Description("Max amount of time in seconds pressing A to wait for a trade to process.")]
    public int MaxTradeConfirmTime { get; set; } = 25;

    [Category(TradeCode), Description("Minimum Link Code.")]
    public int MinTradeCode { get; set; } = 8180;

    [Category(TradeCode), Description("Maximum Link Code.")]
    public int MaxTradeCode { get; set; } = 8199;

    [Category(TradeConfig)]
    [Description("(LGPE/SV) Max amount of time to wait for trade animation to complete")]
    public int TradeAnimationMaxDelaySeconds
    {
        get { return _tradeAnimationMaxDelaySeconds; }
        set { _tradeAnimationMaxDelaySeconds = Math.Max(25, value); }
    }
    private int _tradeAnimationMaxDelaySeconds = 25;

    [Category(Embeds), Description("Settings related to the use of Embeds.")]
    public EmbedSettingsCategory EmbedSettings { get; set; } = new();

    [Category(TradeConfig), Description("Settings related to Dump trades")]
    public DumpSettingsCategory DumpSettings { get; set; } = new();

    [Category(TradeConfig), Description("Miscellaneous Settings")]
    public MiscSettingsCategory MiscSettings { get; set; } = new();

    [Category(TradeConfig), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
    public bool ScreenOff { get; set; }

    [Category(Misc), TypeConverter(typeof(ExpandableObjectConverter))]
    public class EmbedSettingsCategory
    {
        public override string ToString() => "Embed Settings";

        [Category(TradeConfig), Description("Send Embeds with requested Pokémon trades.")]
        public bool UseTradeEmbeds { get; set; } = false;

        [Category(TradeConfig), Description("Use Gender Emoji in place of text in embeds")]
        public bool UseGenderEmojis { get; set; } = false;

        [Category(TradeConfig), Description("Gender Emoji Settings")]
        public GenderEmojiCategory GenderEmojis { get; set; } = new();

        [Category(TradeConfig), Description("Include Tera type emoji with Tera Type.")]
        public bool UseTeraTypeEmojis { get; set; } = false;

        [Category(TradeConfig), Description("Tera Type Emoji Settings")]
        public TeraTypeEmojiCategory TeraTypeEmojis { get; set; } = new();

        [Category(TradeConfig), Description("Include Move type emojis with moves.")]
        public bool UseMoveTypeEmojis { get; set; } = false;

        [Category(TradeConfig), Description("Move Type Emoji Settings")]
        public MoveTypeEmojiCategory MoveTypeEmojis { get; set; } = new();

        [Category(TradeConfig), Description("Use the alternate Embed Layout with Larger Images.")]
        public bool UseAlternateLayout { get; set; } = false;

        [Category(TradeConfig), Description("Include EVs in Embed when applicable.")]
        public bool DisplayEVs { get; set; } = false;

        [Category(TradeConfig), Description("Use TradeStart embeds in place of text.")]
        public bool UseTradeStartEmbeds { get; set; } = false;
    }

    [Category(TradeConfig), TypeConverter(typeof(ExpandableObjectConverter))]
    public class DumpSettingsCategory
    {
        public override string ToString() => "Dump Settings";

        [Category(Dumping), Description("Dump Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(Dumping), Description("Dump Trade: Dumping routine will stop after spending x seconds in trade.")]
        public int MaxDumpTradeTime { get; set; } = 180;

        [Category(Dumping), Description("Dump Trade: Dumping routine will stop after spending x seconds in trade.")]
        public bool DumpTradeLegalityCheck { get; set; } = true;
    }

    [Category(TradeConfig), TypeConverter(typeof(ExpandableObjectConverter))]
    public class MiscSettingsCategory
    {
        public override string ToString() => "Misc. Settings";

        [Category(TradeConfig), Description("Move to the center of the union Room in BDSP for trades.")]
        public bool CenterUnionRoom { get; set; } = false;

        [Category(TradeConfig), Description("Silly, useless feature to post a meme when certain illegal or disallowed trade requests are made.")]
        public bool Memes { get; set; } = false;

        [Category(TradeConfig), Description("Enter either direct picture or gif links, or file names with extensions. For example, file1.png, file2.jpg, etc.")]
        public string MemeFileNames { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Select default species for \"ItemTrade\", if configured.")]
        public Species ItemTradeSpecies { get; set; } = Species.None;

        [Category(TradeCode), Description("Determines the shiny odds of a mystery traded pokemon. 1 in XXX. Default is 100")]
        public int MysteryTradeShinyOdds { get; set; } = 100;
    }

    [Category(TradeConfig), TypeConverter(typeof(ExpandableObjectConverter))]
    public class GenderEmojiCategory
    {
        public override string ToString() => "Gender Emojis";

        [Category(TradeConfig), Description("Enter the Male emoji text \"<:Name:ID#:>\"")]
        public string Male { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Female emoji text \"<:Name:ID#:>\"")]
        public string Female { get; set; } = string.Empty;
    }

    [Category(TradeConfig), TypeConverter(typeof(ExpandableObjectConverter))]
    public class MoveTypeEmojiCategory
    {
        public override string ToString() => "Move Type Emojis";

        [Category(TradeConfig), Description("Enter the Bug Type emoji text \"<:Name:ID#:>\"")]
        public string BugType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Dark Type emoji text \"<:Name:ID#:>\"")]
        public string DarkType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Dragon Type emoji text \"<:Name:ID#:>\"")]
        public string DragonType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Electric Type emoji text \"<:Name:ID#:>\"")]
        public string ElectricType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Fairy Type emoji text \"<:Name:ID#:>\"")]
        public string FairyType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Fighting Type emoji text \"<:Name:ID#:>\"")]
        public string FightingType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Fire Type emoji text \"<:Name:ID#:>\"")]
        public string FireType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Flying Type emoji text \"<:Name:ID#:>\"")]
        public string FlyingType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Ghost Type emoji text \"<:Name:ID#:>\"")]
        public string GhostType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Grass Type emoji text \"<:Name:ID#:>\"")]
        public string GrassType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Ground Type emoji text \"<:Name:ID#:>\"")]
        public string GroundType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Ice Type emoji text \"<:Name:ID#:>\"")]
        public string IceType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Poison Type emoji text \"<:Name:ID#:>\"")]
        public string PoisonType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Psychic Type emoji text \"<:Name:ID#:>\"")]
        public string PsychicType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Normal Type emoji text \"<:Name:ID#:>\"")]
        public string NormalType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Rock Type emoji text \"<:Name:ID#:>\"")]
        public string RockType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Steel Type emoji text \"<:Name:ID#:>\"")]
        public string SteelType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Water Type emoji text \"<:Name:ID#:>\"")]
        public string WaterType { get; set; } = string.Empty;
    }

    [Category(TradeConfig), TypeConverter(typeof(ExpandableObjectConverter))]
    public class TeraTypeEmojiCategory
    {
        public override string ToString() => "Tera Type Emojis";

        [Category(TradeConfig), Description("Enter the Bug Type emoji text \"<:Name:ID#:>\"")]
        public string BugType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Dark Type emoji text \"<:Name:ID#:>\"")]
        public string DarkType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Dragon Type emoji text \"<:Name:ID#:>\"")]
        public string DragonType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Electric Type emoji text \"<:Name:ID#:>\"")]
        public string ElectricType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Fairy Type emoji text \"<:Name:ID#:>\"")]
        public string FairyType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Fighting Type emoji text \"<:Name:ID#:>\"")]
        public string FightingType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Fire Type emoji text \"<:Name:ID#:>\"")]
        public string FireType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Flying Type emoji text \"<:Name:ID#:>\"")]
        public string FlyingType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Ghost Type emoji text \"<:Name:ID#:>\"")]
        public string GhostType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Grass Type emoji text \"<:Name:ID#:>\"")]
        public string GrassType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Ground Type emoji text \"<:Name:ID#:>\"")]
        public string GroundType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Ice Type emoji text \"<:Name:ID#:>\"")]
        public string IceType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Poison Type emoji text \"<:Name:ID#:>\"")]
        public string PoisonType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Psychic Type emoji text \"<:Name:ID#:>\"")]
        public string PsychicType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Normal Type emoji text \"<:Name:ID#:>\"")]
        public string NormalType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Rock Type emoji text \"<:Name:ID#:>\"")]
        public string RockType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Steel Type emoji text \"<:Name:ID#:>\"")]
        public string SteelType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Water Type emoji text \"<:Name:ID#:>\"")]
        public string WaterType { get; set; } = string.Empty;

        [Category(TradeConfig), Description("Enter the Stellar Type emoji text \"<:Name:ID#:>\"")]
        public string StellarType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gets a random trade code based on the range settings.
    /// </summary>
    public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);

    /// <summary>
    /// Gets a random picto trade code for LGPE.
    /// </summary>
    public static List<PictoCodes> GetRandomLGTradeCode()
    {
        var code = new List<PictoCodes>();
        for (int i = 0; i <= 2; i++)
        {
            code.Add((PictoCodes)Util.Rand.Next(10));
        }
        return code;
    }
}