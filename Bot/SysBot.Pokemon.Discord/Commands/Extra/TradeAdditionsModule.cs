using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.Enhancements;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Pokemon.SpecialRequests;

namespace SysBot.Pokemon.Discord
{
    [Summary("Generates and queues various silly trade additions")]
    public class TradeAdditionsModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
        private readonly ExtraCommandUtil<T> Util = new();
        private static bool setsFound = true;

        [Command("giveawayqueue")]
        [Alias("gaq")]
        [Summary("Prints the users in the giveway queues.")]
        [RequireSudo]
        public async Task GetGiveawayListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Giveaways";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("giveawaypool")]
        [Alias("gap")]
        [Summary("Show a list of Pokémon available for giveaway.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task DisplayGiveawayPoolCountAsync()
        {
            var pool = Info.Hub.LedyGA.Pool;
            if (pool.Count > 0)
            {
                var test = pool.Files;
                var lines = pool.Files.Select((z, i) => $"{i + 1}: {z.Key} = {(Species)z.Value.RequestInfo.Species}");
                var msg = string.Join("\n", lines);
                await Util.ListUtil(Context, "Giveaway Pool Details", msg).ConfigureAwait(false);
            }
            else await ReplyAsync("Giveaway pool is empty.").ConfigureAwait(false);
        }

        [Command("GApoolReload")]
        [Alias("gpr")]
        [Summary("Reloads the bot pool from the setting's folder.")]
        [RequireSudo]
        public async Task ReloadGAPoolAsync()
        {
            var me = SysCord<T>.Runner;
            var hub = me.Hub;

            var pool = hub.LedyGA.Pool.Reload(hub.Config.Folder.GiveAwayFolder);
            if (!pool)
                await ReplyAsync("Failed to reload from folder.").ConfigureAwait(false);
            else
                await ReplyAsync($"Reloaded from folder. Giveaway Pool count: {hub.LedyGA.Pool.Count}").ConfigureAwait(false);

        }

        [Command("giveaway")]
        [Alias("ga", "giveme", "gimme")]
        [Summary("Makes the bot trade you the specified giveaway Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task GiveawayAsync([Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await GiveawayAsync(code, content).ConfigureAwait(false);
        }

        [Command("giveaway")]
        [Alias("ga", "giveme", "gimme")]
        [Summary("Makes the bot trade you the specified giveaway Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task GiveawayAsync([Summary("Giveaway Code")] int code, [Remainder] string content)
        {
            T pk;
            content = ReusableActions.StripCodeBlock(content);
            var pool = Info.Hub.LedyGA.Pool;
            if (pool.Count == 0)
            {
                await ReplyAsync("Giveaway pool is empty.").ConfigureAwait(false);
                return;
            }
            else if (content.ToLower() == "random") // Request a random giveaway prize.
            {
                var randomIndex = new Random().Next(pool.Count); // generate a random number between 0 and the number of items in the pool
                pk = pool[randomIndex]; // select the item at the randomly generated index
            }
            else if (Info.Hub.LedyGA.GiveAway.TryGetValue(content, out GiveAwayRequest<T>? val) && val is not null)
                pk = val.RequestInfo;
            else
            {
                await ReplyAsync($"Requested Pokémon not available, use \"{Info.Hub.Config.Discord.CommandPrefix}giveawaypool\" for a full list of available giveaways!").ConfigureAwait(false);
                return;
            }

            var sig = Context.User.GetFavor();
            var lgcode = Info.GetRandomLGTradeCode();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, Context.User, lgcode, true).ConfigureAwait(false);
        }

        [Command("fixOT")]
        [Alias("fix", "f")]
        [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task FixAdOT()
        {
            var code = Info.GetRandomTradeCode();
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.FixOT, PokeTradeType.FixOT, Context.User, lgcode, false).ConfigureAwait(false);
        }

        [Command("fixOT")]
        [Alias("fix", "f")]
        [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task FixAdOT([Summary("Trade Code")] int code)
        {
            var sig = Context.User.GetFavor();
            var lgcode = Info.GetRandomLGTradeCode();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.FixOT, PokeTradeType.FixOT, Context.User, lgcode, false).ConfigureAwait(false);
        }

        [Command("fixOTList")]
        [Alias("fl", "fq")]
        [Summary("Prints the users in the FixOT queue.")]
        [RequireSudo]
        public async Task GetFixListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.FixOT);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("specialrequest")]
        [Alias("specialrequest", "sr")]
        [Summary("Special requests for a Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task SpecialRequestAsync(int code)
        {
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.SpecialRequest, PokeTradeType.SpecialRequest, Context.User, lgcode, false).ConfigureAwait(false);
        }

        [Command("specialrequest")]
        [Alias("specialrequest", "sr")]
        [Summary("Special requests for a Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task SpecialRequestAsync()
        {
            var code = Info.GetRandomTradeCode();
            await SpecialRequestAsync(code).ConfigureAwait(false);
        }

        [Command("specialrequest")]
        [Alias("specialrequest", "sr")]
        [Summary("Special requests for a Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task SpecialRequestAsync(string wcname)
        {
            var code = Info.GetRandomTradeCode();
            await SpecialRequestAsync(code, wcname).ConfigureAwait(false);
        }
        
        [Command("specialrequest")]
        [Alias("specialrequest", "sr")]
        [Summary("Special requests for a Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task SpecialRequestAsync(int code, string wcname)
        {
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();
            var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
            var sav = SaveUtil.GetBlankSAV((GameVersion)trainer.Game, trainer.OT);
            var pk = LoadEvent<T>(wcname.Replace("pls", "").ToLower(), sav, Info.Hub.Config.Folder.SpecialRequestWCFolder);

            if (pk is not null)
            {
                await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.SpecialRequest, PokeTradeType.Specific, Context.User, lgcode, true).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("This isn't a valid request!");
            }
        }

        [Command("specialrequestuser")]
        [Alias("sru")]
        [Summary("Special requests for a Pokémon.")]
        [RequireSudo]
        public async Task SpecialRequestAsync([Summary("Mentioned User")] SocketUser usr, string wcname)
        {
            var code = Info.GetRandomTradeCode();
            await SpecialRequestAsync(usr, code, wcname).ConfigureAwait(false);
        }

        [Command("specialrequestuser")]
        [Alias("sru")]
        [Summary("Special requests for a Pokémon.")]
        [RequireSudo]
        public async Task SpecialRequestAsync(SocketUser usr, int code, string wcname)
        {
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = Context.User.GetFavor();
            var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
            var sav = SaveUtil.GetBlankSAV((GameVersion)trainer.Game, trainer.OT);
            var pk = LoadEvent<T>(wcname.Replace("pls", "").ToLower(), sav, Info.Hub.Config.Folder.SpecialRequestWCFolder);

            if (pk is not null)
            {
                await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.SpecialRequest, PokeTradeType.Specific, usr, lgcode, true, true, true).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("This isn't a valid request!");
            }
        }

        [Command("specialrequestpool")]
        [Alias("srp", "srpool")]
        [Summary("Show a list of Pokémon available for SpecialRequest Distributions.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task DisplaySpecialRequestPoolCountAsync()
        {
            string folderPath = Info.Hub.Config.Folder.SpecialRequestWCFolder;

            if (folderPath == null)
            {
                await ReplyAsync("No Folder Currently Set.");
            }
            else
            {
                var fileNames = Directory.GetFiles(folderPath).Select(Path.GetFileNameWithoutExtension);
                if (fileNames.Any())
                {
                    var lines = fileNames.Select((z, i) => $"{i + 1}: {z}");
                    var msg = string.Join("\n", lines);
                    await Util.ListUtil(Context, "Available SpecialRequests Distributions", msg).ConfigureAwait(false);
                }
                else await ReplyAsync("No files found.").ConfigureAwait(false);
            }
        }

        [Command("requestshowdown")]
        [Alias("rs", "ss")]
        [Summary("Shows Showdown Format Text of attached PKM file.")]
        public async Task ShowdownRequest()
        {
            if (Context.Message.Attachments.Count > 0)
            {
                foreach (var att in Context.Message.Attachments)
                    await Context.Channel.RepostPKMAsShowdownAsync(att).ConfigureAwait(false);
                return;
            }
            else
            {
                await ReplyAsync("Please attach a valid PKM file.").ConfigureAwait(false);
                return;
            }
        }

        [Command("requestdetailedshowdown")]
        [Alias("rds")]
        [Summary("Shows detailed Showdown Format Text of attached PKM file.")]
        public async Task DetailedShowdownRequest()
        {
            if (Context.Message.Attachments.Count > 0)
            {
                foreach (var att in Context.Message.Attachments)
                    await Context.Channel.RepostPKMAsShowdownAsync(att, true).ConfigureAwait(false);
                return;
            }
            else
            {
                await ReplyAsync("Please attach a valid PKM file.").ConfigureAwait(false);
                return;
            }
        }

        [Command("itemTrade")]
        [Alias("it", "item")]
        [Summary("Makes the bot trade you a Pokémon holding the requested item, or Ditto if stat spread keyword is provided.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task ItemTrade([Remainder] string item)
        {
            var code = Info.GetRandomTradeCode();
            await ItemTrade(code, item).ConfigureAwait(false);
        }

        [Command("itemTrade")]
        [Alias("it", "item")]
        [Summary("Makes the bot trade you a Pokémon holding the requested item.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task ItemTrade([Summary("Trade Code")] int code, [Remainder] string item)
        {
            Species species = Info.Hub.Config.Trade.ItemTradeSpecies is Species.None ? Species.Diglett : Info.Hub.Config.Trade.ItemTradeSpecies;
            var set = new ShowdownSet($"{SpeciesName.GetSpeciesNameGeneration((ushort)species, 2, 8)} @ {item.Trim()}");
            var template = AutoLegalityWrapper.GetTemplate(set);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out var result);
            pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
            if (pkm.HeldItem == 0 && !Info.Hub.Config.Trade.Memes)
            {
                await ReplyAsync($"{Context.User.Username}, the item you entered wasn't recognized.").ConfigureAwait(false);
                return;
            }

            var la = new LegalityAnalysis(pkm);
            if (Info.Hub.Config.Trade.Memes && await TrollAsync(Context, pkm is not T || !la.Valid, pkm, true).ConfigureAwait(false))
                return;

            if (pkm is not T pk || !la.Valid)
            {
                var reason = result == "Timeout" ? "That set took too long to generate." : "I wasn't able to create something from that.";
                var imsg = $"Oops! {reason} Here's my best attempt for that {species}!";
                await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
                return;
            }
            pk.ResetPartyStats();

            var sig = Context.User.GetFavor();
            var lgcode = Info.GetRandomLGTradeCode();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true).ConfigureAwait(false);
        }

        [Command("dittoTrade")]
        [Alias("dt", "ditto")]
        [Summary("Makes the bot trade you a Ditto with a requested stat spread and language.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task DittoTrade([Summary("A combination of \"ATK/SPA/SPE\" or \"6IV\"")] string keyword, [Summary("Language")] string language, [Summary("Nature")] string nature)
        {
            var code = Info.GetRandomTradeCode();
            await DittoTrade(code, keyword, language, nature).ConfigureAwait(false);
        }

        [Command("dittoTrade")]
        [Alias("dt", "ditto")]
        [Summary("Makes the bot trade you a Ditto with a requested stat spread and language.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task DittoTrade([Summary("Trade Code")] int code, [Summary("A combination of \"ATK/SPA/SPE\" or \"6IV\"")] string keyword, [Summary("Language")] string language, [Summary("Nature")] string nature)
        {
            keyword = keyword.ToLower().Trim();
            if (Enum.TryParse(language, true, out LanguageID lang))
                language = lang.ToString();
            else
            {
                await Context.Message.ReplyAsync($"Couldn't recognize language: {language}.").ConfigureAwait(false);
                return;
            }

            nature = nature.Trim()[..1].ToUpper() + nature.Trim()[1..].ToLower();
            var set = new ShowdownSet($"{keyword}(Ditto)\nLanguage: {language}\nNature: {nature}");
            var template = AutoLegalityWrapper.GetTemplate(set);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out var result);
            TradeExtensions<T>.DittoTrade((T)pkm);

            var la = new LegalityAnalysis(pkm);
            if (Info.Hub.Config.Trade.Memes && await TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false))
                return;

            if (pkm is not T pk || !la.Valid)
            {
                var reason = result == "Timeout" ? "That set took too long to generate." : "I wasn't able to create something from that.";
                var imsg = $"Oops! {reason} Here's my best attempt for that Ditto!";
                await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
                return;
            }

            pk.ResetPartyStats();
            var sig = Context.User.GetFavor();
            var lgcode = Info.GetRandomLGTradeCode();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true).ConfigureAwait(false);
        }

        [Command("eggTrade")]
        [Alias("et", "egg")]
        [Summary("Makes the bot trade you an egg of the provided pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task EggTradeAsync([Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await EggTradeAsync(code, content).ConfigureAwait(false);
        }

        [Command("eggTrade")]
        [Alias("et", "egg")]
        [Summary("Makes the bot trade you an egg of the provided pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task EggTradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
        {
            var lgcode = Info.GetRandomLGTradeCode();
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out var result);
            bool noEggs = typeof(T) == typeof(PA8) || typeof(T) == typeof(PB7);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            if (!noEggs && Breeding.CanHatchAsEgg(pkm.Species))
            {
                try
                {
                    TradeExtensions<T>.EggTrade(pkm, template);
                    var la = new LegalityAnalysis(pkm);
                    var spec = GameInfo.Strings.Species[template.Species];
                    pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;

                    if (pkm is not T pk || !la.Valid)
                    {
                        var reason = result == "Timeout" ? $"That {spec} set took too long to generate." : result == "VersionMismatch" ? "Request refused: version mismatch." : $"I wasn't able to create a {spec} from that set.";
                        var imsg = $"Oops! {reason}";
                        if (result == "Failed")
                            imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                        await ReplyAsync(imsg).ConfigureAwait(false);
                        return;
                    }
                    pk.ResetPartyStats();

                    var sig = Context.User.GetFavor();
                    await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                    var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                    await ReplyAsync(msg).ConfigureAwait(false);
                }
            }
            else
            {
                if (noEggs)
                {
                    await ReplyAsync($"{(typeof(T) == typeof(PA8) ? "PLA" : "LGPE")} does not have breeding!").ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync("Provided Pokémon cannot be an egg!").ConfigureAwait(false);
                }
            }
        }

        [Command("mysteryegg")]
        [Alias("me", "randomegg", "re")]
        [Summary("Makes the bot trade you an egg of a random Pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task MysteryEggTradeAsync()

        {
            var code = Info.GetRandomTradeCode();
            await MysteryEggTradeAsync(code).ConfigureAwait(false);
        }

        [Command("mysteryegg")]
        [Alias("me", "randomegg", "re")]
        [Summary("Makes the bot trade you an egg of a random Pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task MysteryEggTradeAsync([Summary("Trade Code")] int code)
        {
            // Keep generating a random species until one that can be hatched as an egg is found
            bool foundValidSpecies = false;
            Species randomSpecies = Species.None;

            while (!foundValidSpecies)
            {
                // Get a random species from the list
                Random random = new();
                randomSpecies = (Species)CanHatchFromEgg.ElementAt(random.Next(CanHatchFromEgg.Count));

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
            content += "\n.IVs=$rand\n.Nature=$0,24\nShiny: Yes\n.Moves=$suggest";

            var lgcode = Info.GetRandomLGTradeCode();
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out var result);
            bool noEggs = typeof(T) == typeof(PA8) || typeof(T) == typeof(PB7);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            if (!noEggs && CanBeEgg(pkm.Species))
            {
                try
                {
                    TradeExtensions<T>.EggTrade(pkm, template);
                    
                    var la = new LegalityAnalysis(pkm);
                    var spec = GameInfo.Strings.Species[template.Species];
                    pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;

                    if (pkm is not T pk || !la.Valid)
                    {
                        var reason = result == "Timeout" ? $"That {spec} set took too long to generate." : result == "VersionMismatch" ? "Request refused: version mismatch." : $"I wasn't able to create a {spec} from that set.";
                        var imsg = $"Oops! {reason}";
                        if (result == "Failed")
                            imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                        await ReplyAsync(imsg).ConfigureAwait(false);
                        return;
                    }
                    pk.ResetPartyStats();

                    var sig = Context.User.GetFavor();
                    await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true, false).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                    var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                    await ReplyAsync(msg).ConfigureAwait(false);
                }
            }
            else
            {
                await ReplyAsync($"{(typeof(T) == typeof(PA8) ? "PLA" : "LGPE")} does not have breeding!").ConfigureAwait(false);
            }
        }

        [Command("SupriseTrade")]
        [Alias("sp", "st", "suprisemon", "randommon", "suprise")]
        [Summary("Makes the bot trade you a random Pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task MysteryMonTradeAsync()

        {
            var code = Info.GetRandomTradeCode();
            await MysteryMonTradeAsync(code).ConfigureAwait(false);
        }

        [Command("SupriseTrade")]
        [Alias("sp", "st", "suprisemon", "randommon", "suprise")]
        [Summary("Makes the bot trade you a random Pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task MysteryMonTradeAsync([Summary("Trade Code")] int code)
        {
            bool foundValidSpecies = false;
            Species randomSpecies = Species.None;
            while (!foundValidSpecies)
            {
                Random random = new();
                int randomIndex = random.Next(0, (typeof(T) == typeof(PB7) ? LGPE : typeof(T) == typeof(PK8) ? SWSH : typeof(T) == typeof(PB8) ? BDSP : typeof(T) == typeof(PA8) ? LA : SV).Length);
                int randomInt = (typeof(T) == typeof(PB7) ? LGPE : typeof(T) == typeof(PK8) ? SWSH : typeof(T) == typeof(PB8) ? BDSP : typeof(T) == typeof(PA8) ? LA : SV)[randomIndex];
                randomSpecies = (Species)randomInt;

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
            Random random2 = new();
            var max = Info.Hub.Config.Trade.MysteryTradeShinyOdds < 0 ? 0 : Info.Hub.Config.Trade.MysteryTradeShinyOdds;
            int randomNumber = random2.Next(0, max);
            var shiny = randomNumber == 0 ? true : false;
            content += $"\n.IVs=$rand\n.Nature=$0,24\n{(shiny ? "Shiny: Yes\n" : "")}.Moves=$suggest\n.AbilityNumber=$0,2\n.TeraTypeOverride=$rand\n.Ball=$0,37\n.DynamaxLevel=$0,10\n.TrainerTID7=$0001,3559\n.TrainerSID7=$000001,993401\n.OT_Name=Surprise!\n.GV_ATK=$0,7\n.GV_DEF=$0,7\n.GV_HP=$0,7\n.GV_SPA=$0,7\n.GV_SPD=$0,7\n.GV_SPE=$0,7";

            var lgcode = Info.GetRandomLGTradeCode();
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out _);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            while (true)
            {
                var la2 = new LegalityAnalysis(pkm);

                if (pkm is not T pk || !la2.Valid)
                {
                    pkm = sav.GetLegal(template, out _);
                    continue;
                }
                else
                {
                    pk.ResetPartyStats();
                    var sig = Context.User.GetFavor();
                    await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true, true).ConfigureAwait(false);
                    break;
                }
            }
        }

        [Command("toggleEmbeds")]
        [Alias("te")]
        [Summary("Toggles the 'UseTradeEmbeds' setting.")]
        [RequireSudo]
        public async Task ToggleEmbeds()
        {
            Info.Hub.Config.Trade.UseTradeEmbeds = !Info.Hub.Config.Trade.UseTradeEmbeds;
            await ReplyAsync($"UseTradeEmbeds set to: {Info.Hub.Config.Trade.UseTradeEmbeds}");
        }

        [Command("toggleAutoOT")]
        [Alias("taot", "tot")]
        [Summary("Toggles the 'UseTradeTradePartnerInfo' setting.")]
        [RequireSudo]
        public async Task ToggleAutoOT()
        {
            Info.Hub.Config.Legality.UseTradePartnerInfo = !Info.Hub.Config.Legality.UseTradePartnerInfo;
            await ReplyAsync($"UseTradePartnerInfo set to: {Info.Hub.Config.Legality.UseTradePartnerInfo}");
        }

        [Command("peek")]
        [Summary("Take and send a screenshot from the first available bot.")]
        [RequireSudo]
        public async Task Peek()
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var bot = SysCord<T>.Runner.GetBot(IPBotHelper<T>.Get(SysCord<T>.Runner));
            if (bot == null)
            {
                await ReplyAsync($"No bots available to take a screenshot.").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            var bytes = await c.PixelPeek(token).ConfigureAwait(false);
            if (bytes.Length == 1)
            {
                await ReplyAsync($"Failed to take a screenshot for bot at {bot.Bot.Config.Connection.IP}. Is the bot connected?").ConfigureAwait(false);
                return;
            }
            MemoryStream ms = new(bytes);

            var img = "cap.jpg";
            var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = Color.Purple }.WithFooter(new EmbedFooterBuilder { Text = $"Captured image from bot at address {bot.Bot.Config.Connection.IP}." });
            await Context.Channel.SendFileAsync(ms, img, "", false, embed: embed.Build());
        }

        [Command("peek")]
        [Summary("Take and send a screenshot from the specified Switch.")]
        [RequireSudo]
        public async Task Peek(string address)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var bot = SysCord<T>.Runner.GetBot(address);
            if (bot == null)
            {
                await ReplyAsync($"No bot found with the specified address ({address}).").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            var bytes = await c.PixelPeek(token).ConfigureAwait(false);
            if (bytes.Length == 1)
            {
                await ReplyAsync($"Failed to take a screenshot for bot at {address}. Is the bot connected?").ConfigureAwait(false);
                return;
            }
            MemoryStream ms = new(bytes);

            var img = "cap.jpg";
            var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = Color.Purple }.WithFooter(new EmbedFooterBuilder { Text = $"Captured image from bot at address {address}." });
            await Context.Channel.SendFileAsync(ms, img, "", false, embed: embed.Build());
        }

        [Command("smogon")]
        [Alias("gs")]
        [Summary("Generates Smogon sets of the provided pokemon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task GetSmogonSets([Summary("Showdown Set")][Remainder] string content)
        {
            var messages = GenerateSmogonSets(content);
            if (setsFound)
            {
                await Context.Message.DeleteAsync().ConfigureAwait(false);
                await Context.User.SendMessageAsync("Here is what I found for the Requested Pokémon!").ConfigureAwait(false);

                var builder = new StringBuilder();
                foreach (var message in messages)
                {
                    if (builder.Length + message.Length + 1 > 2000)
                    {
                        await Context.User.SendMessageAsync(builder.ToString()).ConfigureAwait(false);
                        builder.Clear();
                    }
                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }
                    builder.Append(message);
                }
                if (builder.Length > 0)
                {
                    await Context.User.SendMessageAsync(builder.ToString()).ConfigureAwait(false);
                }
            }
            else
            {
                await ReplyAsync("No Smogon sets were found for the specified Pokémon.").ConfigureAwait(false);
            }
        }

        public static async Task<bool> TrollAsync(SocketCommandContext context, bool invalid, PKM pkm, bool itemTrade = false)
        {
            var rng = new Random();
            bool noItem = pkm.HeldItem == 0 && itemTrade;
            var path = Info.Hub.Config.Trade.MemeFileNames.Split(',');
            if (Info.Hub.Config.Trade.MemeFileNames == "" || path.Length == 0)
                path = new string[] { "https://i.imgur.com/qaCwr09.png" }; //If memes enabled but none provided, use a default one.

            if (invalid || !ItemRestrictions.IsHeldItemAllowed(pkm) || noItem || (pkm.Nickname.ToLower() == "egg" && !Breeding.CanHatchAsEgg(pkm.Species)))
            {
                if (noItem)
                {
                    await context.Channel.SendMessageAsync($"{context.User.Username}, the item you entered wasn't recognized.").ConfigureAwait(false);
                }
                else
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Oops!")
                        .WithDescription($"I wasn't able to create that {GameInfo.Strings.Species[pkm.Species]}.\nHere's a meme instead!")
                        .WithImageUrl(path[rng.Next(path.Length)])
                        .WithColor(Color.Green)
                        .Build();
                    await context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }

        public static List<string> GenerateSmogonSets(string content)
        {
            content = ReusableActions.StripCodeBlock(content);
            var pkset = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(pkset);
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out _);
            var sets = SmogonSetList.GenerateSets(pkm);

            if (sets == null || sets.Count == 0)
            {
                setsFound = false;
                return new List<string> { };
            }

            setsFound = true;
            var messages = new List<string>();
            foreach (var set in sets)
            {
                messages.Add("```" + set.Text + "```");
            }
            return messages;
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

        public int[] LGPE = {
            001,002,003,004,005,006,007,008,009,010,011,012,013,014,015,016,017,018,019,020,021,022,023,024,025,026,027,
            028,030,031,033,034,035,036,037,038,039,040,041,042,043,044,045,046,047,048,049,050,051,052,053,054,055,056,
            057,058,059,060,061,062,063,064,065,066,067,068,069,070,071,072,073,074,075,076,077,078,079,080,081,082,084,
            085,086,087,088,089,090,091,092,093,094,095,096,097,098,099,100,101,102,103,104,105,106,107,108,109,110,111,
            112,113,114,115,116,117,118,119,120,121,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,
            140,141,142,143,144,145,146,147,148,149,150,151,808,809 };

        public int[] SWSH = {
            001,002,003,004,005,006,007,008,009,010,011,012,025,026,027,028,030,031,033,034,035,036,037,038,039,040,041,042,043,044,
            045,050,051,052,053,054,055,058,059,060,61,62,63,64,65,66,67,68,72,73,77,78,79,80,81,82,90,91,92,93,94,95,98,99,102,103,
            104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,123,124,125,126,127,128,129,130,131,132,133,134,
            135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,163,164,169,170,171,172,173,174,175,176,177,178,182,
            183,184,185,186,194,195,196,197,199,202,206,208,211,212,213,214,215,220,221,222,223,224,225,226,227,230,233,236,237,238,
            239,240,241,242,243,244,245,246,247,248,249,251,252,253,254,255,256,257,258,259,260,263,264,270,271,272,273,274,275,278,
            279,280,281,282,290,291,292,293,294,295,298,302,303,304,305,306,309,310,315,318,319,320,321,324,328,329,330,333,334,337,
            338,339,340,341,342,343,344,345,346,347,348,349,350,355,356,359,360,361,362,363,364,365,369,371,372,373,374,375,376,377,
            378,379,380,381,382,383,384,385,403,404,405,406,407,415,416,420,421,422,423,425,426,427,428,434,435,436,437,438,440,442,
            443,444,445,446,447,448,449,450,451,452,453,454,458,459,460,461,462,463,464,465,466,467,468,470,471,473,475,477,478,479,
            480,481,482,483,484,485,486,487,488,494,506,507,508,509,510,517,518,519,520,521,524,525,526,527,528,529,530,531,532,533,
            534,535,536,537,538,539,543,544,545,546,547,548,549,550,551,552,553,554,555,556,557,558,559,560,561,562,563,564,565,566,
            567,568,569,570,571,572,573,574,575,576,577,578,579,582,583,584,587,588,589,590,591,592,593,595,596,597,598,599,600,601,
            605,606,607,608,609,610,611,612,613,614,615,616,617,618,619,620,621,622,623,624,625,626,627,628,629,630,631,632,633,634,
            635,636,637,638,639,640,641,642,643,644,645,646,647,649,659,660,661,662,663,674,675,677,678,679,680,681,682,683,684,685,
            686,687,688,689,690,691,692,693,694,695,696,697,698,699,700,701,702,703,704,705,706,707,708,709,710,711,712,713,714,715,
            716,717,718,719,721,722,723,724,725,726,727,728,729,730,736,737,738,742,743,744,745,746,747,748,749,750,751,752,753,754,
            755,756,757,758,759,760,761,762,763,764,765,766,767,768,769,770,771,773,776,777,778,780,781,785,786,787,788,789,790,791,
            792,793,794,795,796,797,798,799,800,801,802,803,804,805,806,807,808,809,810,811,812,813,814,815,816,817,818,819,820,821,
            822,823,824,825,826,827,828,829,830,831,832,833,834,835,836,837,838,839,840,841,842,843,844,845,846,847,848,849,850,851,
            852,853,854,855,856,857,858,859,860,861,862,863,864,867,868,869,870,871,872,873,874,875,876,877,878,879,880,881,882,883,
            884,885,886,887,888,889,890,891,892,893,894,895,896,897,898 };

        public int[] BDSP = {
            001,002,003,004,005,006,007,008,009,010,011,012,013,014,015,016,017,018,019,020,021,022,023,024,025,026,027,028,030,
            031,033,034,035,036,037,038,039,040,041,042,043,044,045,046,047,048,049,050,051,052,053,054,055,056,057,058,059,060,
            061,062,063,064,065,066,067,068,069,070,071,072,073,074,075,076,077,078,079,080,081,082,084,085,086,087,088,089,090,
            091,092,093,094,095,096,097,098,099,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,
            120,121,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,
            150,151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,177,178,
            179,180,181,182,183,184,185,186,187,188,189,190,191,192,193,194,195,196,197,198,199,200,201,202,203,204,205,206,207,
            208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,
            237,238,239,240,241,242,243,244,245,246,247,248,249,251,252,253,254,255,256,257,258,259,260,261,262,263,264,265,266,
            267,268,269,270,271,272,273,274,275,276,277,278,279,280,281,282,283,284,285,286,287,288,289,290,291,292,293,294,295,
            296,297,298,299,300,301,302,303,304,305,306,307,308,309,310,311,312,313,314,315,316,317,318,319,320,321,322,323,324,
            325,326,327,328,329,330,331,332,333,334,335,336,337,338,339,340,341,342,343,344,345,346,347,348,349,350,351,352,353,
            354,355,356,357,358,359,360,361,362,363,364,365,366,367,368,369,370,371,372,373,374,375,376,377,378,379,380,381,382,
            383,384,385,386,387,388,389,390,391,392,393,394,395,396,397,398,399,400,401,402,403,404,405,406,407,408,409,410,411,
            412,413,414,415,416,417,418,419,420,421,422,423,424,425,426,427,428,429,430,431,432,433,434,435,436,437,438,440,441,
            442,443,444,445,446,447,448,449,450,451,452,453,454,455,456,457,458,459,460,461,462,463,464,465,466,467,468,469,470,
            471,472,473,475,476,477,478,479,480,481,482,483,484,485,486,487,488,489,490,491,492,493, };

        public int[] LA = {
            025,026,035,036,037,038,041,042,046,047,054,055,063,064,065,066,067,068,072,073,074,075,076,077,078,081,082,092,093,094,
            095,108,111,112,113,114,123,125,126,129,130,133,134,135,136,137,143,155,156,169,172,173,175,176,185,190,193,196,197,198,
            200,201,207,208,212,214,215,216,217,220,221,223,224,226,233,234,239,240,242,265,266,267,268,269,280,281,282,299,315,339,
            340,355,356,358,361,362,363,364,365,387,388,389,390,391,392,393,394,395,396,397,398,399,400,401,402,403,404,405,406,407,
            408,409,410,411,412,413,414,415,416,417,418,419,420,421,422,423,424,425,426,427,428,429,430,431,432,433,434,435,436,437,
            438,440,441,442,443,444,445,446,447,448,449,450,451,452,453,454,455,456,457,458,459,460,461,462,463,464,465,466,467,468,
            469,470,471,472,473,475,476,477,478,479,480,481,482,483,484,485,486,487,488,489,490,491,492,493,501,502,548,627,641,642,
            645,700,704,712,722,723,899,900,901,902,903,904,905, };

        public int[] SV = {
            004,005,006,025,026,039,040,048,049,050,051,052,053,054,055,056,057,058,059,079,080,081,082,088,089,090,091,092,093,094,
            096,097,100,101,113,123,128,129,130,132,133,134,135,136,144,145,146,147,148,149,150,151,155,156,157,172,174,179,180,181,
            183,184,185,187,188,189,191,192,194,195,196,197,198,199,200,203,204,205,206,211,212,214,215,216,217,225,228,229,231,232,
            234,242,246,247,248,278,279,280,281,282,283,284,285,286,287,288,289,296,297,298,302,307,308,316,317,322,323,324,325,326,
            331,332,333,334,335,336,339,340,353,354,357,361,362,370,371,372,373,382,383,384,396,397,398,401,402,403,404,405,415,416,
            417,418,419,422,423,425,426,429,430,434,435,436,437,438,440,442,443,444,445,447,448,449,450,453,454,456,457,459,460,461,
            462,470,471,475,478,479,480,481,482,483,484,485,487,488,493,501,502,503,548,549,550,551,552,553,570,571,574,575,576,585,
            586,590,591,594,602,603,604,610,611,612,613,614,615,624,625,627,628,633,634,635,636,637,641,642,645,648,650,651,652,653,
            654,655,656,657,658,661,662,663,664,665,666,667,668,669,670,671,672,673,690,691,692,693,700,701,702,703,704,705,706,707,
            712,713,714,715,719,720,721,722,723,724,734,735,739,740,741,744,745,747,748,749,750,753,754,757,758,761,762,763,765,766,
            769,770,775,778,779,801,810,811,812,813,814,815,816,817,818,819,820,821,822,823,833,834,837,838,839,840,841,842,843,844,
            846,847,848,849,854,855,856,857,858,859,860,861,863,870,871,872,873,874,875,876,878,879,885,886,887,888,889,890,891,892,
            893,894,895,896,897,898,899,900,901,902,903,904,905,906,907,908,909,910,911,912,913,914,915,916,917,918,919,920,921,922,
            923,924,925,926,927,928,929,930,931,932,933,934,935,936,937,938,939,940,941,942,943,944,945,946,947,948,949,950,951,952,
            953,954,955,956,957,958,959,960,961,962,963,964,965,966,967,968,969,970,971,972,973,974,975,976,977,978,979,980,981,982,
            983,984,985,986,987,988,989,990,991,992,993,994,995,996,997,998,999,1000,1005,1006,1007,1008,1009,1010, };
    }
}