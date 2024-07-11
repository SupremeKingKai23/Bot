using System.ComponentModel;

namespace SysBot.Pokemon;

public class CountSettings : ICountSettings
{
    private const string Counts = nameof(Counts);
    public override string ToString() => "Trade Counts and Settings";

    private int _completedSurprise;
    private int _completedDistribution;
    private int _completedTrades;
    private int _completedSeedChecks;
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

    [Category(Counts), Description("Completed Seed Check Trades")]
    public int CompletedSeedChecks
    {
        get => _completedSeedChecks;
        set => _completedSeedChecks = value;
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
    public void AddCompletedSeedCheck() => Interlocked.Increment(ref _completedSeedChecks);
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
        if (CompletedSeedChecks != 0)
            yield return $"Seed Check Trades: {CompletedSeedChecks}";
        if (CompletedClones != 0)
            yield return $"Clone Trades: {CompletedClones}";
        if (CompletedDumps != 0)
            yield return $"Dump Trades: {CompletedDumps}";
        if (CompletedTrades != 0)
            yield return $"Link Trades: {CompletedTrades}";
        if (CompletedDistribution != 0)
            yield return $"Distribution Trades: {CompletedDistribution}";
        if (CompletedFixOTs != 0)
            yield return $"FixOT Trades: {CompletedFixOTs}";
        if (CompletedSurprise != 0)
            yield return $"Surprise Trades: {CompletedSurprise}";
        if (CompletedSupportTrades != 0)
            yield return $"Support Trades: {CompletedSupportTrades}";
        if (CompletedSpecialRequests != 0)
            yield return $"SpecialRequest Trades: {CompletedSpecialRequests}";
    }
}