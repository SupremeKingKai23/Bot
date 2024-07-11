﻿using PKHeX.Core;

namespace SysBot.Pokemon;

public class TradeQueueManager<T> where T : PKM, new()
{
    private readonly PokeTradeHub<T> Hub;

    private readonly PokeTradeQueue<T> Trade = new(PokeTradeType.Specific);
    private readonly PokeTradeQueue<T> Seed = new(PokeTradeType.Seed);
    private readonly PokeTradeQueue<T> Clone = new(PokeTradeType.Clone);
    private readonly PokeTradeQueue<T> Dump = new(PokeTradeType.Dump);
    private readonly PokeTradeQueue<T> FixOT = new(PokeTradeType.FixOT);
    private readonly PokeTradeQueue<T> Giveaway = new(PokeTradeType.Giveaway);
    private readonly PokeTradeQueue<T> SpecialRequest = new(PokeTradeType.SpecialRequest);
    public readonly TradeQueueInfo<T> Info;
    public readonly PokeTradeQueue<T>[] AllQueues;

    public TradeQueueManager(PokeTradeHub<T> hub)
    {
        Hub = hub;
        Info = new TradeQueueInfo<T>(hub);
        AllQueues = [Seed, Dump, Clone, Trade, FixOT, SpecialRequest, Giveaway];

        foreach (var q in AllQueues)
            q.Queue.Settings = hub.Config.Favoritism;
    }

    public PokeTradeQueue<T> GetQueue(PokeRoutineType type) => type switch
    {
        PokeRoutineType.SeedCheck => Seed,
        PokeRoutineType.Clone => Clone,
        PokeRoutineType.Dump => Dump,
        PokeRoutineType.FixOT => FixOT,
        PokeRoutineType.SpecialRequest => SpecialRequest,
        _ => Trade,
    };

    public void ClearAll()
    {
        foreach (var q in AllQueues)
            q.Clear();
    }

    public bool TryDequeueLedy(out PokeTradeDetail<T> detail, bool force = false)
    {
        detail = default!;
        var cfg = Hub.Config.Distribution;
        if (!cfg.DistributeWhileIdle && !force && !cfg.DistributeWhileIdleME)
            return false;

        if (!cfg.DistributeWhileIdleME && Hub.Ledy.Pool.Count == 0)
            return false;

        if (cfg.DistributeWhileIdleME && !cfg.DistributeWhileIdle && typeof(T) != typeof(PA8) && typeof(T) != typeof(PB7))
        {
            _ = TradeExtensions<T>.MysteryEgg(out var pkm);
            var code2 = cfg.RandomCode ? Hub.Config.Trade.GetRandomTradeCode() : cfg.TradeCode;
            var lgcode2 = TradeSettings.GetRandomLGTradeCode();
            var trainer2 = new PokeTradeTrainerInfo("Random Distribution");
            detail = new PokeTradeDetail<T>(pkm, trainer2, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random, code2, lgcode2, false);
            return true;
        }

        var random = Hub.Ledy.Pool.GetRandomPoke();
        var code = cfg.RandomCode ? Hub.Config.Trade.GetRandomTradeCode() : cfg.TradeCode;
        var lgcode = cfg.RandomCode ? TradeSettings.GetRandomLGTradeCode() : GetLGPETradeCode();
        var trainer = new PokeTradeTrainerInfo("Random Distribution");
        detail = new PokeTradeDetail<T>(random, trainer, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random, code, lgcode, false);
        return true;
    }

    public bool TryDequeue(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
    {
        if (type == PokeRoutineType.FlexTrade)
            return GetFlexDequeue(out detail, out priority);

        return TryDequeueInternal(type, out detail, out priority);
    }

    private bool TryDequeueInternal(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
    {
        var queue = GetQueue(type);
        return queue.TryDequeue(out detail, out priority);
    }

    private bool GetFlexDequeue(out PokeTradeDetail<T> detail, out uint priority)
    {
        var cfg = Hub.Config.Queues;
        if (cfg.FlexMode == FlexYieldMode.LessCheatyFirst)
            return GetFlexDequeueOld(out detail, out priority);
        return GetFlexDequeueWeighted(cfg, out detail, out priority);
    }

    private bool GetFlexDequeueWeighted(QueueSettings cfg, out PokeTradeDetail<T> detail, out uint priority)
    {
        PokeTradeQueue<T>? preferredQueue = null;
        long bestWeight = 0; // prefer higher weights
        uint bestPriority = uint.MaxValue; // prefer smaller
        foreach (var q in AllQueues)
        {
            var peek = q.TryPeek(out detail, out priority);
            if (!peek)
                continue;

            // priority queue is a min-queue, so prefer smaller priorities
            if (priority > bestPriority)
                continue;

            var count = q.Count;
            var time = detail.Time;
            var weight = cfg.GetWeight(count, time, q.Type);

            if (priority >= bestPriority && weight <= bestWeight)
                continue; // not good enough to be preferred over the other.

            // this queue has the most preferable priority/weight so far!
            bestWeight = weight;
            bestPriority = priority;
            preferredQueue = q;
        }

        if (preferredQueue == null)
        {
            detail = default!;
            priority = default;
            return false;
        }

        return preferredQueue.TryDequeue(out detail, out priority);
    }

    private bool GetFlexDequeueOld(out PokeTradeDetail<T> detail, out uint priority)
    {
        if (TryDequeueInternal(PokeRoutineType.SeedCheck, out detail, out priority))
            return true;
        if (TryDequeueInternal(PokeRoutineType.Clone, out detail, out priority))
            return true;
        if (TryDequeueInternal(PokeRoutineType.Dump, out detail, out priority))
            return true;
        if (TryDequeueInternal(PokeRoutineType.LinkTrade, out detail, out priority))
            return true;
        if (TryDequeueInternal(PokeRoutineType.FixOT, out detail, out priority))
            return true;
        if (TryDequeueInternal(PokeRoutineType.SpecialRequest, out detail, out priority))
            return true;
        return false;
    }

    public void Enqueue(PokeRoutineType type, PokeTradeDetail<T> detail, uint priority)
    {
        var queue = GetQueue(type);
        queue.Enqueue(detail, priority);
    }

    // hook in here if you want to forward the message elsewhere???
    public readonly List<Action<PokeRoutineExecutorBase, PokeTradeDetail<T>>> Forwarders = [];

    public void StartTrade(PokeRoutineExecutorBase b, PokeTradeDetail<T> detail)
    {
        foreach (var f in Forwarders)
            f.Invoke(b, detail);
    }

    public List<PictoCodes> GetLGPETradeCode()
    {
        var code = new List<PictoCodes>
    {
        Hub.Config.Distribution.LGPETradeCode.Mon1,
        Hub.Config.Distribution.LGPETradeCode.Mon2,
        Hub.Config.Distribution.LGPETradeCode.Mon3
    };
        return code;
    }
}