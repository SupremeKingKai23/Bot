using Discord;
using Discord.Commands;
using PKHeX.Core;
using PKHeX.Core.Enhancements;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Generates and queues various silly trade additions")]
public class TradeAdditionsModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private readonly ExtraCommandUtil<T> Util = new();
    private static bool setsFound = true;
    private static BotLanguage Lang => Info.Hub.Config.CurrentLanguage;

    [Command("FixOT")]
    [Alias("fix", "f")]
    [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task FixAdOT()
    {
        var code = Info.GetRandomTradeCode();
        var lgcode = Info.GetRandomLGTradeCode();
        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, new T(), PokeRoutineType.FixOT, PokeTradeType.FixOT, Context.User, lgcode, true).ConfigureAwait(false);
    }

    [Command("FixOT")]
    [Alias("fix", "f")]
    [Summary("Fixes OT and Nickname of a Pokémon you show via Link Trade if an advert is detected.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task FixAdOT([Summary("Trade Code")] int code)
    {
        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();
        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, new T(), PokeRoutineType.FixOT, PokeTradeType.FixOT, Context.User, lgcode, true).ConfigureAwait(false);
    }

    [Command("FixOTList")]
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

    [Command("ItemTrade")]
    [Alias("it", "item")]
    [Summary("Makes the bot trade you a Pokémon holding the requested item, or Ditto if stat spread keyword is provided.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task ItemTrade([Summary("Item Name")][Remainder] string item)
    {
        var code = Info.GetRandomTradeCode();
        await ItemTrade(code, item).ConfigureAwait(false);
    }

    [Command("ItemTrade")]
    [Alias("it", "item")]
    [Summary("Makes the bot trade you a Pokémon holding the requested item.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task ItemTrade([Summary("Trade Code")] int code, [Summary("Item Name")][Remainder] string item)
    {
        Species species = Info.Hub.Config.Trade.MiscSettings.ItemTradeSpecies is Species.None ? Species.Diglett : Info.Hub.Config.Trade.MiscSettings.ItemTradeSpecies;
        var set = new ShowdownSet($"{SpeciesName.GetSpeciesNameGeneration((ushort)species, 2, 8)} @ {item.Trim()}");
        var template = AutoLegalityWrapper.GetTemplate(set);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var pkm = sav.GetLegal(template, out var result);
        pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
        if (pkm.HeldItem == 0 && !Info.Hub.Config.Trade.MiscSettings.Memes)
        {
            await ReplyAsync($"{Context.User.Mention}, " + LanguageHelper.BadItem(Lang)).ConfigureAwait(false);
            return;
        }

        var la = new LegalityAnalysis(pkm);
        if (Info.Hub.Config.Trade.MiscSettings.Memes && await TrollAsync(Context, pkm is not T || !la.Valid, pkm, true).ConfigureAwait(false))
            return;

        if (pkm is not T pk || !la.Valid)
        {
            var reason = result switch
            {
                "Timeout" => LanguageHelper.SimpleTimeout(Lang),
                _ => LanguageHelper.SimpleUnable(Lang)
            };
            var oops = LanguageHelper.Oops(Lang);
            var imsg = $"{oops} {reason} " + LanguageHelper.BestAttempt(Lang, species);
            await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
            return;
        }
        pk.ResetPartyStats();

        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();
        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true).ConfigureAwait(false);
    }

    [Command("DittoTrade")]
    [Alias("dt", "ditto")]
    [Summary("Makes the bot trade you a Ditto with a requested stat spread and language.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task DittoTrade([Summary("A combination of \"ATK/SPA/SPE\" or \"6IV\"")] string keyword, [Summary("Language")] string language, [Summary("Nature")] string nature)
    {
        var code = Info.GetRandomTradeCode();
        await DittoTrade(code, keyword, language, nature).ConfigureAwait(false);
    }

    [Command("DittoTrade")]
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
            await Context.Message.ReplyAsync(LanguageHelper.BadLanguage(Lang, language)).ConfigureAwait(false);
            return;
        }

        nature = nature.Trim()[..1].ToUpper() + nature.Trim()[1..].ToLower();
        var set = new ShowdownSet($"{keyword}(Ditto)\nLanguage: {language}\nNature: {nature}");
        var template = AutoLegalityWrapper.GetTemplate(set);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var pkm = sav.GetLegal(template, out var result);
        TradeExtensions<T>.DittoTrade((T)pkm);

        var la = new LegalityAnalysis(pkm);
        if (Info.Hub.Config.Trade.MiscSettings.Memes && await TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false))
            return;

        if (pkm is not T pk || !la.Valid)
        {
            var reason = result switch
            {
                "Timeout" => LanguageHelper.SimpleTimeout(Lang),
                _ => LanguageHelper.SimpleTimeout(Lang)
            };
            var oops = LanguageHelper.Oops(Lang);
            var imsg = $"{oops} {reason}" + LanguageHelper.BestDitto(Lang);
            await Context.Channel.SendPKMAsync(pkm, imsg).ConfigureAwait(false);
            return;
        }

        pk.ResetPartyStats();
        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();
        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true).ConfigureAwait(false);
    }

    [Command("EggTrade")]
    [Alias("et", "egg")]
    [Summary("Makes the bot trade you an egg of the provided pokemon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task EggTradeAsync([Summary("Showdown Set")][Remainder] string content)
    {
        var code = Info.GetRandomTradeCode();
        await EggTradeAsync(code, content).ConfigureAwait(false);
    }

    [Command("EggTrade")]
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
            var msg = LanguageHelper.ParseSet(Lang) + $"\n{string.Join("\n", set.InvalidLines)}";
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
                    var reason = result switch
                    {
                        "Timeout" => LanguageHelper.Timeout(Lang, spec),
                        "VersionMismatch" => LanguageHelper.Mismatch(Lang),
                        _ => LanguageHelper.Unable(Lang, spec)
                    };
                    var oops = LanguageHelper.Oops(Lang);
                    var imsg = $"{oops} {reason}";
                    if (result == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                    await ReplyAsync(imsg).ConfigureAwait(false);
                    return;
                }
                pk.ResetPartyStats();

                var sig = Context.User.GetFavor();
                await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = LanguageHelper.Unexpected(Lang) + $"\n```{string.Join("\n", set.GetSetLines())}```";
                await ReplyAsync(msg).ConfigureAwait(false);
            }
        }
        else
        {
            if (noEggs)
            {
                await ReplyAsync($"{(typeof(T) == typeof(PA8) ? "PLA" : "LGPE")}" + LanguageHelper.NoBreed(Lang)).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync(LanguageHelper.NoEgg(Lang)).ConfigureAwait(false);
            }
        }
    }

    [Command("Smogon")]
    [Alias("gs")]
    [Summary("Generates Smogon sets of the provided pokemon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task GetSmogonSets([Summary("Pokémon")][Remainder] string mon)
    {
        var messages = GenerateSmogonSets(mon);
        if (setsFound)
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            await Context.User.SendMessageAsync(Lang == BotLanguage.Español ? $"Esto es lo que encontré para {mon}" : $"# Here is what I found for {mon}!").ConfigureAwait(false);

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
            await ReplyAsync(Lang == BotLanguage.Español ? "No se encontró ningún set en Smogon para el Pokémon especificado." : "No Smogon sets were found for the specified Pokémon.").ConfigureAwait(false);
        }
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
            return [];
        }
        var titles = SmogonSetList.GetTitles(pkm);
        setsFound = true;
        var messages = new List<string>();

        for (int i = 0; i < sets.Count; i++)
        {
            var set = sets[i];
            var title = titles.ElementAt(i);
            messages.Add($"## {title}\n```" + set.Text + "```");
        }
        return messages;
    }

    public static async Task<bool> TrollAsync(SocketCommandContext context, bool invalid, PKM pkm, bool itemTrade = false)
    {
        var rng = new Random();
        bool noItem = pkm.HeldItem == 0 && itemTrade;
        var path = Info.Hub.Config.Trade.MiscSettings.MemeFileNames.Split(',');
        if (Info.Hub.Config.Trade.MiscSettings.MemeFileNames == "" || path.Length == 0)
            path = ["https://i.imgur.com/qaCwr09.png"]; //If memes enabled but none provided, use a default one.

        if (invalid || !ItemRestrictions.IsHeldItemAllowed(pkm) || noItem || (pkm.Nickname.ToLower() == "egg" && !Breeding.CanHatchAsEgg(pkm.Species)))
        {
            if (noItem)
            {
                await context.Channel.SendMessageAsync($"{context.User.Username}, " + LanguageHelper.BadItem(Lang)).ConfigureAwait(false);
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithTitle(LanguageHelper.Oops(Lang))
                    .WithDescription(LanguageHelper.MemeSet(Lang, GameInfo.Strings.Species[pkm.Species]))
                    .WithImageUrl(path[rng.Next(path.Length)])
                    .WithColor(Color.Green)
                    .Build();
                await context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                return true;
            }
        }
        return false;
    }
}