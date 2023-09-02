using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace SysBot.Pokemon
{
    public static class SpecialRequests
    {
        public enum SpecialTradeType
        {
            None,
            ItemReq,
            BallReq,
            SanitizeReq,
            StatChange,
            Shinify,
            WonderCard,
            TeraChange,
            FailReturn
        }

        public static SpecialTradeType CheckItemRequest<T>(ref T pk, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail, string TrainerName, uint TID, uint SID, SaveFile sav, string folder) where T : PKM, new()
        {
            var sst = SpecialTradeType.None;
            int startingHeldItem = pk.HeldItem;

            //log
            GameStrings str = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
            var allitems = str.GetItemStrings((EntityContext)8, GameVersion.SWSH);
            if (startingHeldItem > 0 && startingHeldItem < allitems.Length)
            {
                var itemHeld = allitems[startingHeldItem];
                caller.Log("Item held: " + itemHeld);
            }
            else
                caller.Log("Held item was outside the bounds of the Array, or nothing was held: " + startingHeldItem);

            int heldItemNew = 1; // master

            if (pk.HeldItem >= 2 && pk.HeldItem <= 4) // ultra<>pokeball
            {
                switch (pk.HeldItem)
                {
                    case 2: //ultra
                        pk.ClearNickname();
                        pk.OT_Name = TrainerName;
                        pk.DisplayTID = TID;
                        pk.DisplaySID = SID;
                        break;
                    case 3: //great
                        pk.OT_Name = TrainerName;
                        pk.DisplayTID = TID;
                        pk.DisplaySID = SID;
                        break;
                    case 4: //poke
                        pk.ClearNickname();
                        break;
                }

                pk.SetRecordFlags(pk.Moves);
                pk.HeldItem = heldItemNew; //free master

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.SanitizeReq;
            }
            else if (pk.Nickname.StartsWith("Clear") && typeof(T) == typeof(PA8))
            {
                switch (pk.Nickname.ToLower())
                {
                    case string s when s.Contains("Both"):
                        pk.ClearNickname();
                        pk.OT_Name = TrainerName;
                        pk.DisplayTID = TID;
                        pk.DisplaySID = SID;
                        break;

                    case string s when s.Contains("OT"):
                        pk.ClearNickname();
                        pk.OT_Name = TrainerName;
                        pk.DisplayTID = TID;
                        pk.DisplaySID = SID;
                        break;

                    case string s when s.Contains("Nick"):
                        pk.ClearNickname();
                        break;
                }

                pk.SetRecordFlags(pk.Moves);
                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.SanitizeReq;
            }
            else if (pk.Nickname.ToLower().Contains("male"))
            {
                switch (pk.Nickname.ToLower())
                {
                    case string s when s.Contains("!male"):
                        pk.Gender = 0;
                        break;

                    case string s when s.Contains("!female"):
                        pk.Gender = 1;
                        break;
                }

                pk.SetRecordFlags(pk.Moves);
                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.StatChange;
            }
            else if (pk.Nickname.Contains("pls"))
            {
                T? loaded = LoadEvent<T>(pk.Nickname.Replace("pls", "").ToLower(), sav, folder);

                if (loaded != null)
                    pk = loaded;
                else
                {
                    detail.SendNotification(caller, "This isn't a valid request!");
                    sst = SpecialTradeType.FailReturn;
                    return sst;
                }

                sst = SpecialTradeType.WonderCard;
            }
            else if ((pk.HeldItem >= 18 && pk.HeldItem <= 22) || pk.IsEgg) // antidote <> awakening (21) <> paralyze heal (22)
            {
                if (pk.HeldItem == 22)
                    pk.SetUnshiny();
                else
                {
                    var type = Shiny.AlwaysStar; // antidote or ice heal
                    if (pk.HeldItem == 19 || pk.HeldItem == 21 || pk.IsEgg) // burn heal or awakening
                        type = Shiny.AlwaysSquare;
                    if (pk.HeldItem == 20 || pk.HeldItem == 21) // ice heal or awakening or fh 
                        pk.IVs = new int[] { 31, 31, 31, 31, 31, 31 };

                    if (typeof(T) == typeof(PK8))
                    {
                        if (pk.IsEgg)
                        {
                            CommonEdits.SetShiny(pk, type);
                        }
                        else
                        {
                            try
                            {

                                string[] MaxLairLegendaries = new string[47] { "144", "145", "146", "150", "243", "244", "245", "249", "250", "380", "381", "382", "383", "384", "480", "481", "482", "483", "484", "485", "487", "488", "641", "642", "643", "644", "645", "646", "716", "717", "718", "785", "786", "787", "788", "791", "792", "793", "794", "795", "796", "797", "798", "799", "800", "805", "806" };
                                uint shinyForm = (uint)(pk.TID16 ^ pk.SID16 ^ ((pk.PID >> 16) ^ (pk.PID & 0xFFFF)));
                                int tidsid = int.Parse(pk.DisplaySID.ToString("D4") + pk.DisplayTID.ToString("D6"));
                                int TID5 = Math.Abs(tidsid % 65536);
                                int SID5 = Math.Abs(tidsid / 65536);

                                if (type == Shiny.AlwaysStar)
                                    CommonEdits.SetShiny(pk, type);
                                // Set square shiny
                                else if (type == Shiny.AlwaysSquare)
                                    pk.PID = (uint)(((TID5 ^ SID5 ^ (pk.PID & 0xFFFF) ^ 0u) << 16) | (pk.PID & 0xFFFF));
                                // Set special star shiny
                                if (MaxLairLegendaries.Contains($"{pk.Species}") && (shinyForm < 16) && (pk.Form != 1))
                                    pk.PID = (uint)(((TID5 ^ SID5 ^ (pk.PID & 0xFFFF) ^ 1u) << 16) | (pk.PID & 0xFFFF));
                            }
                            catch
                            {
                                CommonEdits.SetShiny(pk, type);
                            }
                        }
                    }
                    else
                    {
                        CommonEdits.SetShiny(pk, type);
                    }
                }

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                if (!pk.IsEgg)
                {
                    pk.HeldItem = heldItemNew; //free master
                    pk.SetRecordFlags(pk.Moves);
                }
                sst = SpecialTradeType.Shinify;
            }
            else if (pk.Nickname.StartsWith("Make") && typeof(T) == typeof(PA8))
            {
                var type = Shiny.AlwaysStar;
                switch (pk.Nickname.ToLower())
                {
                    case string s when s.Contains("normal"):
                        pk.ClearNickname();
                        pk.SetUnshiny();
                        break;

                    case string s when s.Contains("shiny6"):
                        pk.ClearNickname();
                        type = Shiny.AlwaysSquare;
                        pk.IVs = new int[] { 31, 31, 31, 31, 31, 31 };
                        break;

                    case string s when s.Contains("shiny"):
                        pk.ClearNickname();
                        type = Shiny.AlwaysSquare;
                        break;
                }
                CommonEdits.SetShiny(pk, type);

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                if (!pk.IsEgg)
                {
                    pk.SetRecordFlags(pk.Moves);
                }
                sst = SpecialTradeType.Shinify;
            }
            else if ((pk.HeldItem >= 30 && pk.HeldItem <= 32) || pk.HeldItem == 27 || pk.HeldItem == 28 || pk.HeldItem == 63) // fresh water/pop/lemonade <> full heal(27) <> revive(28) <> pokedoll(63)
            {
                if (pk.HeldItem == 27)
                    pk.IVs = new int[] { 31, 31, 31, 31, 31, 31 };
                if (pk.HeldItem == 28)
                    pk.IVs = new int[] { 31, 0, 31, 0, 31, 31 };
                if (pk.HeldItem == 30)
                    pk.IVs = new int[] { 31, 0, 31, 31, 31, 31 };
                if (pk.HeldItem == 31)
                    pk.CurrentLevel = 100;
                if (pk.HeldItem == 32)
                {
                    pk.IVs = new int[] { 31, 31, 31, 31, 31, 31 };
                    pk.CurrentLevel = 100;
                }

                if (pk.HeldItem == 63)
                    pk.IVs = new int[] { 31, 31, 31, 0, 31, 31 };

                // clear hyper training from IV switched mons
                if (pk is IHyperTrain iht)
                    iht.HyperTrainClear();

                pk.SetRecordFlags(pk.Moves);
                pk.HeldItem = heldItemNew; //free master

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.StatChange;
            }
            else if (pk.Nickname.StartsWith("Stats") && typeof(T) == typeof(PA8))
            {
                switch (pk.Nickname.ToLower())
                {
                    case string s when s.Contains("6iv"):
                        pk.ClearNickname();
                        pk.IVs = new int[] { 31, 31, 31, 31, 31, 31 };
                        break;

                    case string s when s.Contains("5ivs"):
                        pk.ClearNickname();
                        pk.IVs = new int[] { 31, 31, 31, 0, 31, 31 };
                        break;

                    case string s when s.Contains("5iva"):
                        pk.ClearNickname();
                        pk.IVs = new int[] { 31, 0, 31, 31, 31, 31 };
                        break;

                    case string s when s.Contains("4iv"):
                        pk.ClearNickname();
                        pk.IVs = new int[] { 31, 0, 31, 0, 31, 31 };
                        break;

                    case string s when s.Contains("lvl100"):
                        pk.ClearNickname();
                        pk.CurrentLevel = 100;
                        break;

                    case string s when s.Contains("max"):
                        pk.ClearNickname();
                        pk.IVs = new int[] { 31, 31, 31, 31, 31, 31 };
                        pk.CurrentLevel = 100;
                        break;
                }

                // clear hyper training from IV switched mons
                if (pk is IHyperTrain iht)
                    iht.HyperTrainClear();

                pk.SetRecordFlags(pk.Moves);

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.StatChange;
            }
            else if (pk.HeldItem >= 55 && pk.HeldItem <= 62) // guard spec <> x sp.def
            {
                switch (pk.HeldItem)
                {
                    case 55: // guard spec
                        pk.Language = (int)LanguageID.Japanese;
                        break;
                    case 56: // dire hit
                        pk.Language = (int)LanguageID.English;
                        break;
                    case 57: // x atk
                        pk.Language = (int)LanguageID.German;
                        break;
                    case 58: // x def
                        pk.Language = (int)LanguageID.French;
                        break;
                    case 59: // x spe
                        pk.Language = (int)LanguageID.Spanish;
                        break;
                    case 60: // x acc
                        pk.Language = (int)LanguageID.Korean;
                        break;
                    case 61: // x spatk
                        pk.Language = (int)LanguageID.ChineseT;
                        break;
                    case 62: // x spdef
                        pk.Language = (int)LanguageID.ChineseS;
                        break;
                }

                pk.ClearNickname();

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                pk.SetRecordFlags(pk.Moves);
                pk.HeldItem = heldItemNew; //free master
                sst = SpecialTradeType.SanitizeReq;
            }
            else if (pk.Nickname.Contains("Lang") && typeof(T) == typeof(PA8))
            {
                switch (pk.Nickname.ToUpper())
                {
                    case string s when s.Contains("JPN"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.Japanese;
                        break;
                    case string s when s.Contains("ENG"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.English;
                        break;
                    case string s when s.Contains("GER"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.German;
                        break;
                    case string s when s.Contains("FRE"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.French;
                        break;
                    case string s when s.Contains("ESP"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.Spanish;
                        break;
                    case string s when s.Contains("ITA"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.Italian;
                        break;
                    case string s when s.Contains("KOR"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.Korean;
                        break;
                    case string s when s.Contains("CHT"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.ChineseT;
                        break;
                    case string s when s.Contains("CHS"):
                        pk.ClearNickname();
                        pk.Language = (int)LanguageID.ChineseS;
                        break;
                }

                pk.ClearNickname();

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                pk.SetRecordFlags(pk.Moves);

                sst = SpecialTradeType.SanitizeReq;
            }
            else if (pk.HeldItem >= 1231 && pk.HeldItem <= 1251) // lonely mint <> serious mint
            {
                GameStrings strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
                var items = strings.GetItemStrings((EntityContext)8, GameVersion.SWSH);
                var itemName = items[pk.HeldItem];
                var natureName = itemName.Split(' ')[0];
                var natureEnum = Enum.TryParse(natureName, out Nature result);
                if (natureEnum)
                    pk.Nature = pk.StatNature = (int)result;
                else
                {
                    detail.SendNotification(caller, "Nature request was not found in the db.");
                    sst = SpecialTradeType.FailReturn;
                    return sst;
                }

                pk.SetRecordFlags(pk.Moves);
                pk.HeldItem = heldItemNew; //free master

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.StatChange;

            }
            else if (pk.Nickname.Contains("Nat") && typeof(T) == typeof(PA8))
            {
                switch (pk.Nickname.ToLower())
                {
                    case string s when s.Contains("adamant"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Adamant;
                        break;
                    case string s when s.Contains("bold"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Bold;
                        break;
                    case string s when s.Contains("brave"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Brave;
                        break;
                    case string s when s.Contains("calm"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Calm;
                        break;
                    case string s when s.Contains("careful"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Careful;
                        break;
                    case string s when s.Contains("gentle"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Gentle;
                        break;
                    case string s when s.Contains("hasty"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Hasty;
                        break;
                    case string s when s.Contains("impish"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Impish;
                        break;
                    case string s when s.Contains("jolly"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Jolly;
                        break;
                    case string s when s.Contains("lax"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Lax;
                        break;
                    case string s when s.Contains("lonely"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Lonely;
                        break;
                    case string s when s.Contains("mild"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Mild;
                        break;
                    case string s when s.Contains("modest"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Modest;
                        break;
                    case string s when s.Contains("naive"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Naive;
                        break;
                    case string s when s.Contains("naughty"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Naughty;
                        break;
                    case string s when s.Contains("quiet"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Quiet;
                        break;
                    case string s when s.Contains("rash"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Rash;
                        break;
                    case string s when s.Contains("relaxed"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Relaxed;
                        break;
                    case string s when s.Contains("sassy"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Sassy;
                        break;
                    case string s when s.Contains("serious"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Serious;
                        break;
                    case string s when s.Contains("timid"):
                        pk.ClearNickname();
                        pk.Nature = pk.StatNature = (int)Nature.Timid;
                        break;
                }

                pk.SetRecordFlags(pk.Moves);

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.StatChange;

            }
            else if (pk.HeldItem >= 1862 && pk.HeldItem <= 1879)
            {
                if (pk is PK9 pk9)
                    switch (pk.HeldItem)
                    {
                        case 1862: // Normal Tera Shard
                            pk9.TeraTypeOverride = MoveType.Normal;
                            break;
                        case 1863: // Fire Tera Shard
                            pk9.TeraTypeOverride = MoveType.Fire;
                            break;
                        case 1864: // Water Tera Shard
                            pk9.TeraTypeOverride = MoveType.Water;
                            break;
                        case 1865: // Electric Tera Shard
                            pk9.TeraTypeOverride = MoveType.Electric;
                            break;
                        case 1866: // Grass Tera Shard
                            pk9.TeraTypeOverride = MoveType.Grass;
                            break;
                        case 1867: // Ice Tera Shard
                            pk9.TeraTypeOverride = MoveType.Ice;
                            break;
                        case 1868: // Fighting Tera Shard
                            pk9.TeraTypeOverride = MoveType.Fighting;
                            break;
                        case 1869: // Poison Tera Shard
                            pk9.TeraTypeOverride = MoveType.Poison;
                            break;
                        case 1870: // Ground Tera Shard
                            pk9.TeraTypeOverride = MoveType.Ground;
                            break;
                        case 1871: // Flying Tera Shard
                            pk9.TeraTypeOverride = MoveType.Flying;
                            break;
                        case 1872: // Psychic Tera Shard
                            pk9.TeraTypeOverride = MoveType.Psychic;
                            break;
                        case 1873: // Bug Tera Shard
                            pk9.TeraTypeOverride = MoveType.Bug;
                            break;
                        case 1874: // Rock Tera Shard
                            pk9.TeraTypeOverride = MoveType.Rock;
                            break;
                        case 1875: // Ghost Tera Shard
                            pk9.TeraTypeOverride = MoveType.Ghost;
                            break;
                        case 1876: // Dragon Tera Shard
                            pk9.TeraTypeOverride = MoveType.Dragon;
                            break;
                        case 1877: // Dark Tera Shard
                            pk9.TeraTypeOverride = MoveType.Dark;
                            break;
                        case 1878: // Steel Tera Shard
                            pk9.TeraTypeOverride = MoveType.Steel;
                            break;
                        case 1879: // Fairy Tera Shard
                            pk9.TeraTypeOverride = MoveType.Fairy;
                            break;

                    }

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                pk.SetRecordFlags(pk.Moves);
                pk.HeldItem = heldItemNew; //free master
                sst = SpecialTradeType.TeraChange;
            }
            else if (pk.Nickname.StartsWith("!") && typeof(T) != typeof(PA8))
            {
                var itemLookup = pk.Nickname.Substring(1).Replace(" ", string.Empty);
                GameStrings strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
                var items = strings.GetItemStrings((EntityContext)8, GameVersion.SWSH);
                int item = Array.FindIndex(items, z => z.Replace(" ", string.Empty).StartsWith(itemLookup, StringComparison.OrdinalIgnoreCase));
                if (item < 0)
                {
                    detail.SendNotification(caller, "Item request was invalid. Check spelling & gen.");
                    return sst;
                }

                pk.HeldItem = item;

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.ItemReq;
            }
            else if (pk.Nickname.StartsWith("?") && typeof(T) != typeof(PA8) || pk.Nickname.StartsWith("？") && typeof(T) != typeof(PA8))
            {
                var itemLookup = pk.Nickname.Substring(1).Replace(" ", string.Empty);
                GameStrings strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
                var balls = strings.balllist;

                int item = Array.FindIndex(balls, z => z.Replace(" ", string.Empty).StartsWith(itemLookup, StringComparison.OrdinalIgnoreCase));
                if (item < 0)
                {
                    detail.SendNotification(caller, "Ball request was invalid. Check spelling & generation.");
                    return sst;
                }

                pk.Ball = item;

                LegalizeIfNotLegal(ref pk, caller, detail, TrainerName);

                sst = SpecialTradeType.BallReq;
            }
            else
                return sst;
            return sst;
        }

        public static T? LoadEvent<T>(string v, SaveFile sav, string srwc) where T : PKM, new()
        {
            T? toRet = null;
            byte[] wc = new byte[1];
            string type = "fail";

            string[] extensions = { ".wc9", ".wc8", ".wa8", ".wb8", ".wc7", ".wc6", ".pgf", ".pcd" };

            foreach (string extension in extensions)
            {
                string[] files = Directory.GetFiles(srwc, "*" + v + "*" + extension);
                if (files.Length > 0)
                {
                    string pathwc = files[0];
                    wc = File.ReadAllBytes(pathwc);
                    type = extension.Substring(1); // Remove the dot from the extension
                    break;
                }
            }

            if (type == "fail")
                return null;

            MysteryGift? loadedwc = null;
            if (wc.Length != 1)
                loadedwc = LoadWC(wc, type);
            if (loadedwc != null)
            {
                var pkloaded = loadedwc.ConvertToPKM(sav);

                pkloaded = EntityConverter.ConvertToType(pkloaded, typeof(T), out _);
                if (pkloaded != null)
                {
                    pkloaded.CurrentHandler = 1;
                }

                if (pkloaded != null)
                    toRet = (T)pkloaded;
            }

            return toRet;
        }

        public static MysteryGift? LoadWC(byte[] data, string suffix = "wc9")
        {
            return suffix switch
            {
                "wc9" => new WC9(data),
                "wa8" => new WA8(data),
                "wb8" => new WB8(data),
                "wc8" => new WC8(data),
                "wc7" => new WC7(data),
                "wc6" => new WC6(data),
                "pgf" => new PGF(data),
                "pcd" => new PCD(data),
                _ => null
            };
        }

        private static void LegalizeIfNotLegal<T>(ref T pkm, PokeRoutineExecutor<T> caller, PokeTradeDetail<T> detail, string trainerName) where T : PKM, new()
        {
            var tempPk = pkm.Clone();

            var la = new LegalityAnalysis(pkm);
            if (!la.Valid)
            {
                detail.SendNotification(caller, "This request isn't legal! Attemping to legalize...");
                caller.Log(la.Report());
                try
                {
                    pkm = (T)pkm.LegalizePokemon();
                }
                catch (Exception e)
                {
                    caller.Log("Legalization failed: " + e.Message); return;
                }
            }
            else
                return;

            pkm.OT_Name = tempPk.OT_Name;

            la = new LegalityAnalysis(pkm);
            if (!la.Valid)
            {
                pkm = (T)pkm.LegalizePokemon();
            }
        }
    }
}
