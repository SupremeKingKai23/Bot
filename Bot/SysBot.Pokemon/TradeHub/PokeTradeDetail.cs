using PKHeX.Core;

namespace SysBot.Pokemon;

public class PokeTradeDetail<TPoke>(TPoke pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, PokeTradeType type, int code, List<PictoCodes> lgcode, bool favored = false, bool mysteryEgg = false) : IEquatable<PokeTradeDetail<TPoke>>, IFavoredEntry where TPoke : PKM, new()
{
    // ReSharper disable once StaticMemberInGenericType
    /// <summary> Global variable indicating the amount of trades created. </summary>
    private static int CreatedCount;
    /// <summary> Indicates if this trade data should be given priority for queue insertion. </summary>
    public bool IsFavored { get; } = favored;

    public bool MysteryEgg { get; } = mysteryEgg;

    /// <summary>
    /// Trade Code
    /// </summary>
    public readonly int Code = code;

    /// <summary> Data to be traded </summary>
    public TPoke TradeData = pkm;

    /// <summary> Trainer details </summary>
    public readonly PokeTradeTrainerInfo Trainer = info;

    /// <summary> Destination to be notified for status updates </summary>
    public readonly IPokeTradeNotifier<TPoke> Notifier = notifier;

    /// <summary> Type of trade this object is for </summary>
    public readonly PokeTradeType Type = type;

    /// <summary> Time the object was created at </summary>
    public readonly DateTime Time = DateTime.Now;
    /// <summary> Unique incremented ID </summary>
    public readonly int ID = Interlocked.Increment(ref CreatedCount) % 3000;

    /// <summary> Indicates if the trade data should be synchronized with other bots. </summary>
    public bool IsSynchronized => Type == PokeTradeType.Random;

    /// <summary> Indicates if the trade failed at least once and is being tried again. </summary>
    public bool IsRetry;

    /// <summary> Indicates if the trade data is currently being traded. </summary>
    public bool IsProcessing;

    /// <summary> LGPE Picto Trade Code </summary>
    public List<PictoCodes> LGPETradeCode = lgcode;

    public void TradeInitialize(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeInitialize(routine, this);
    public void TradeSearching(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeSearching(routine, this);
    public void TradeCanceled(PokeRoutineExecutor<TPoke> routine, PokeTradeResult msg) => Notifier.TradeCanceled(routine, this, msg);

    public virtual void TradeFinished(PokeRoutineExecutor<TPoke> routine, TPoke result)
    {
        Notifier.TradeFinished(routine, this, result);
    }

    public void SendNotification(PokeRoutineExecutor<TPoke> routine, string message) => Notifier.SendNotification(routine, this, message);
    public void SendNotification(PokeRoutineExecutor<TPoke> routine, PokeTradeSummary obj) => Notifier.SendNotification(routine, this, obj);
    public void SendNotification(PokeRoutineExecutor<TPoke> routine, TPoke obj, string message) => Notifier.SendNotification(routine, this, obj, message);
    public void SendNotification(PokeRoutineExecutor<TPoke> routine, string title, string message) => Notifier.SendNotification(routine, this, title, message);

    public bool Equals(PokeTradeDetail<TPoke>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ReferenceEquals(Trainer, other.Trainer);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((PokeTradeDetail<TPoke>)obj);
    }

    public override int GetHashCode() => Trainer.GetHashCode();
    public override string ToString() => $"{Trainer.TrainerName} - {Code}";

    public string Summary(int queuePosition)
    {
        if (TradeData.Species == 0)
            return $"{queuePosition:00}: {Trainer.TrainerName}";
        return $"{queuePosition:00}: {Trainer.TrainerName}, {(Species)TradeData.Species}";
    }
}

public enum PictoCodes
{
    Pikachu,
    Eevee,
    Bulbasaur,
    Charmander,
    Squirtle,
    Pidgey,
    Caterpie,
    Rattata,
    Jigglypuff,
    Diglett
}