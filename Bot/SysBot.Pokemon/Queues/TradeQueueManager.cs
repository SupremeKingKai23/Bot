using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class TradeQueueManager<T> where T : PKM, new()
    {
        private readonly PokeTradeHub<T> Hub;

        private readonly PokeTradeQueue<T> Trade = new(PokeTradeType.Specific);
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
            AllQueues = new[] { Dump, Clone, Trade, FixOT, SpecialRequest, Giveaway };

            foreach (var q in AllQueues)
                q.Queue.Settings = hub.Config.Favoritism;
        }

        public PokeTradeQueue<T> GetQueue(PokeRoutineType type) => type switch
        {
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

            if (cfg.DistributeWhileIdleME && !cfg.DistributeWhileIdle)
            {
                // Keep generating a random species until one that can be hatched as an egg is found
                bool foundValidSpecies = false;
                Species randomSpecies = Species.None;

                while (!foundValidSpecies)
                {
                    // Get a random species from the list
                    Random random2 = new();
                    randomSpecies = (Species)CanHatchFromEgg.ElementAt(random2.Next(CanHatchFromEgg.Count));

                    // Generate a legal Pokemon for the random species
                    var sav2 = AutoLegalityWrapper.GetTrainerInfo<T>();
                    var set2 = new ShowdownSet(randomSpecies.ToString());
                    var pkm2 = sav2.GetLegal(set2, out _);
                    var la = new LegalityAnalysis(pkm2);

                    if (la.Valid)
                    {
                        foundValidSpecies = true;
                    }
                }

                var content = randomSpecies.ToString();
                content += "\n.IVs=$rand\n.Nature=$0,24\nShiny: Yes\n.Moves=$suggest\n.AbilityNumber=$0,2";
                var set = new ShowdownSet(content);
                var template = AutoLegalityWrapper.GetTemplate(set);
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                T pkm = (T)sav.GetLegal(template, out _);
                bool pla = typeof(T) == typeof(PA8);
                if (set.InvalidLines.Count != 0)
                {
                    return false;
                }

                if (!pla && CanBeEgg(pkm.Species))
                {
                    try
                    {
                        TradeExtensions<T>.EggTrade(pkm, template);
                        var la = new LegalityAnalysis(pkm);
                        var spec = GameInfo.Strings.Species[template.Species];
                        pkm = (T)(EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm);

                        if (pkm is not T pk || !la.Valid)
                        {
                            return false;
                        }
                        pk.ResetPartyStats();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                var code2 = cfg.RandomCode ? Hub.Config.Trade.GetRandomTradeCode() : cfg.TradeCode;
                var lgcode2 = cfg.RandomCode ? TradeSettings.GetRandomLGTradeCode() : Hub.Config.Distribution.GetLGPETradeCode();
                var trainer2 = new PokeTradeTrainerInfo("Random Distribution");
                detail = new PokeTradeDetail<T>(pkm, trainer2, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random, code2, lgcode2, false);
                return true;
            }

            var random = Hub.Ledy.Pool.GetRandomPoke();
            var code = cfg.RandomCode ? Hub.Config.Trade.GetRandomTradeCode() : cfg.TradeCode;
            var lgcode = cfg.RandomCode ? TradeSettings.GetRandomLGTradeCode() : Hub.Config.Distribution.GetLGPETradeCode();
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
        public readonly List<Action<PokeRoutineExecutorBase, PokeTradeDetail<T>>> Forwarders = new();

        public void StartTrade(PokeRoutineExecutorBase b, PokeTradeDetail<T> detail)
        {
            foreach (var f in Forwarders)
                f.Invoke(b, detail);
        }

        public static bool CanBeEgg(ushort species)
        {
            return CanHatchFromEgg.Contains(species);
        }

        public static readonly HashSet<ushort> CanHatchFromEgg = new()
        {
            001, 004, 007, 010, 013, 016, 019, 021, 023, 027, 029, 032, 037, 039, 041, 043, 046, 048, 050, 052, 054, 056, 058, 060,
            063, 066, 069, 072, 074, 077, 079, 081, 083, 084, 086, 088, 090, 092, 095, 096, 098, 100, 102, 104, 108, 109, 111, 114,
            115, 116, 118, 120, 123, 126, 127, 128, 129, 131, 133, 137, 138, 140, 147, 142, 152, 155, 158, 161, 163, 165, 167, 170,
            172, 173, 174, 175, 177, 179, 183, 187, 190, 191, 193, 194, 198, 200, 203, 204, 206, 207, 209, 211, 213, 214, 215, 216,
            218, 220, 222, 223, 225, 227, 228, 231, 234, 235, 238, 239, 240, 241, 246, 252, 255, 258, 261, 263, 265, 270, 273, 276,
            278, 280, 283, 285, 287, 290, 293, 296, 298, 299, 300, 302, 303, 304, 307, 309, 311, 312, 313, 314, 316, 318, 320, 322,
            324, 325, 327, 328, 331, 333, 335, 337, 338, 339, 341, 343, 345, 347, 349, 351, 352, 353, 355, 357, 358, 359, 360, 361,
            363, 366, 369, 370, 371, 374, 387, 390, 393, 396, 399, 401, 403, 406, 408, 410, 412, 415, 417, 418, 420, 422, 425, 427,
            431, 433, 434, 436, 438, 439, 440, 441, 442, 443, 447, 449, 451, 453, 455, 456, 459, 479, 489, 495, 498, 501, 504, 506,
            509, 511, 513, 515, 517, 519, 522, 524, 527, 529, 531, 532, 535, 538, 539, 540, 543, 546, 548, 550, 551, 554, 557, 559,
            561, 562, 564, 566, 568, 570, 572, 574, 577, 580, 582, 585, 587, 590, 592, 595, 597, 599, 602, 605, 607, 610, 613, 615,
            616, 618, 619, 621, 622, 624, 626, 627, 629, 631, 632, 633, 636, 650, 653, 656, 659, 661, 664, 667, 669, 672, 674, 677,
            679, 682, 684, 686, 688, 690, 692, 694, 696, 698, 701, 702, 703, 704, 707, 708, 710, 712, 714, 722, 725, 728, 731, 734,
            736, 738, 739, 742, 744, 746, 748, 749, 751, 753, 755, 757, 759, 761, 764, 765, 766, 767, 769, 771, 774, 775, 776, 777,
            778, 779, 780, 781, 782, 810, 813, 816, 819, 821, 824, 827, 829, 831, 833, 835, 837, 840, 843, 845, 846, 848, 850, 852,
            854, 856, 859, 868, 870, 871, 872, 874, 875, 876, 877, 878, 885, 906, 909, 912, 915, 917, 919, 921, 924, 926, 928, 931,
            932, 935, 938, 940, 942, 944, 946, 948, 950, 951, 953, 955, 957, 960, 962, 963, 965, 967, 969, 971, 973, 974, 976, 978,
            996,
        };
    }
}
