using PKHeX.Core;
using SysBot.Base;
using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TradeSettings : IBotStateSettings, ICountSettings
    {
        private const string TradeCode = nameof(TradeCode);
        private const string TradeConfig = nameof(TradeConfig);
        private const string Dumping = nameof(Dumping);
        private const string Counts = nameof(Counts);
        public override string ToString() => "Trade Bot Settings";

        [Category(TradeConfig), Description("Time to wait for a trade partner in seconds.")]
        public int TradeWaitTime { get; set; } = 30;

        [Category(TradeConfig), Description("Max amount of time in seconds pressing A to wait for a trade to process.")]
        public int MaxTradeConfirmTime { get; set; } = 25;

        [Category(TradeCode), Description("Minimum Link Code.")]
        public int MinTradeCode { get; set; } = 8180;

        [Category(TradeCode), Description("Maximum Link Code.")]
        public int MaxTradeCode { get; set; } = 8199;

        [Category(TradeConfig), Description("(LGPE) Max amount of time to wait for trade animation to complete")]
        public int TradeAnimationMaxDelaySeconds = 25;

        [Category(Dumping), Description("Dump Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(Dumping), Description("Dump Trade: Dumping routine will stop after spending x seconds in trade.")]
        public int MaxDumpTradeTime { get; set; } = 180;

        [Category(Dumping), Description("Dump Trade: Dumping routine will stop after spending x seconds in trade.")]
        public bool DumpTradeLegalityCheck { get; set; } = true;

        [Category(TradeConfig), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; }

        [Category(TradeConfig), Description("Send Embeds with requested Pokémon trades.")]
        public bool UseTradeEmbeds { get; set; } = false;

        [Category(TradeConfig), Description("Use the alternate Embed Layout with Larger Images.")]
        public bool UseAlternateLayout { get; set; } = false;

        [Category(TradeConfig), Description("Select default species for \"ItemTrade\", if configured.")]
        public Species ItemTradeSpecies { get; set; } = Species.None;

        [Category(TradeCode), Description("Determines the shiny odds of a mystery traded pokemon. 1 in XXX. Default is 100")]
        public int MysteryTradeShinyOdds { get; set; } = 100;

        [Category(TradeConfig), Description("Silly, useless feature to post a meme when certain illegal or disallowed trade requests are made.")]
        public bool Memes { get; set; } = false;

        [Category(TradeConfig), Description("Enter either direct picture or gif links, or file names with extensions. For example, file1.png, file2.jpg, etc.")]
        public string MemeFileNames { get; set; } = string.Empty;

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

        private int _completedSurprise;
        private int _completedDistribution;
        private int _completedTrades;
        private int _completedClones;
        private int _completedDumps;
        private int _completedFixOTs;
        private int _completedSpecialRequests;
        private int _completedSupportTrades;

        [Category(Counts), Description("Completed Surprise Trades")]
        public int CompletedSurprise
        {
            get => _completedSurprise;
            set => _completedSurprise = value;
        }

        [Category(Counts), Description("Completed Link Trades (Distribution)")]
        public int CompletedDistribution
        {
            get => _completedDistribution;
            set => _completedDistribution = value;
        }

        [Category(Counts), Description("Completed Link Trades (Specific User)")]
        public int CompletedTrades
        {
            get => _completedTrades;
            set => _completedTrades = value;
        }

        [Category(Counts), Description("Completed Clone Trades (Specific User)")]
        public int CompletedClones
        {
            get => _completedClones;
            set => _completedClones = value;
        }

        [Category(Counts), Description("Completed Dump Trades (Specific User)")]
        public int CompletedDumps
        {
            get => _completedDumps;
            set => _completedDumps = value;
        }

        [Category(Counts), Description("Completed FixOT Trades (Specific User)")]
        public int CompletedFixOTs
        {
            get => _completedFixOTs;
            set => _completedFixOTs = value;
        }

        [Category(Counts), Description("Completed SpecialRequest Trades (Specific User)")]
        public int CompletedSpecialRequests
        {
            get => _completedSpecialRequests;
            set => _completedSpecialRequests = value;
        }

        [Category(Counts), Description("Completed Support Trades (Specific User)")]
        public int CompletedSupportTrades
        {
            get => _completedSupportTrades;
            set => _completedSupportTrades = value;        
        }

        [Category(Counts), Description("When enabled, the counts will be emitted when a status check is requested.")]
        public bool EmitCountsOnStatusCheck { get; set; }

        public void AddCompletedTrade() => Interlocked.Increment(ref _completedTrades);
        public void AddCompletedSurprise() => Interlocked.Increment(ref _completedSurprise);
        public void AddCompletedDistribution() => Interlocked.Increment(ref _completedDistribution);
        public void AddCompletedDumps() => Interlocked.Increment(ref _completedDumps);
        public void AddCompletedClones() => Interlocked.Increment(ref _completedClones);
        public void AddCompletedFixOTs() => Interlocked.Increment(ref _completedFixOTs);
        public void AddCompletedSpecialRequests() => Interlocked.Increment(ref _completedSpecialRequests);
        public void AddCompletedSupportTrades() => Interlocked.Increment(ref _completedSupportTrades);

        public IEnumerable<string> GetNonZeroCounts()
        {
            if (!EmitCountsOnStatusCheck)
                yield break;
            if (CompletedClones != 0)
                yield return $"Clone Trades: {CompletedClones}";
            if (CompletedDumps != 0)
                yield return $"Dump Trades: {CompletedDumps}";
            if (CompletedTrades != 0)
                yield return $"Link Trades: {CompletedTrades}";
            if (CompletedDistribution != 0)
                yield return $"Distribution Trades: {CompletedDistribution}";
            if (CompletedSurprise != 0)
                yield return $"Surprise Trades: {CompletedSurprise}";
            if (CompletedFixOTs != 0)
                yield return $"FixOT Trades: {CompletedFixOTs}";
            if (CompletedSupportTrades != 0)
                yield return $"Support Trades: {CompletedSupportTrades}";
            if (CompletedSpecialRequests != 0)
                yield return $"SpecialRequest Trades: {CompletedSpecialRequests}";
        }
    }
}
