using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Commands for Giveawy Pokémon.")]
public class GiveawayModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private readonly ExtraCommandUtil<T> Util = new();
    private static BotLanguage Lang => Info.Hub.Config.CurrentLanguage;

    [Command("GiveawayQueue")]
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

    [Command("GiveawayPool")]
    [Alias("gpool", "gap")]
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

    [Command("GAPoolReload")]
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
            await ReplyAsync($"Reloaded from Giveaway folder. Giveaway Pool count: {hub.LedyGA.Pool.Count}").ConfigureAwait(false);

    }

    [Command("Giveaway")]
    [Alias("ga", "giveme", "gimme")]
    [Summary("Makes the bot trade you the specified giveaway Pokémon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
    public async Task GiveawayAsync([Summary("Giveaway Request")][Remainder] string content)
    {
        var code = Info.GetRandomTradeCode();
        await GiveawayAsync(code, content).ConfigureAwait(false);
    }

    [Command("Giveaway")]
    [Alias("ga", "giveme", "gimme")]
    [Summary("Makes the bot trade you the specified giveaway Pokémon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
    public async Task GiveawayAsync([Summary("Giveaway Code")] int code, [Summary("Giveaway Request")][Remainder] string content)
    {
        T pk;
        content = ReusableActions.StripCodeBlock(content);
        var pool = Info.Hub.LedyGA.Pool;
        if (pool.Count == 0)
        {
            await ReplyAsync("Giveaway pool is empty.").ConfigureAwait(false);
            return;
        }
        else if (content.Equals("random", StringComparison.CurrentCultureIgnoreCase)) // Request a random giveaway prize.
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
        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, Context.User, lgcode, true).ConfigureAwait(false);
    }

    [Command("AddGiveawayPokemon")]
    [Alias("addgap", "agap")]
    [Summary("Adds supplied PKM file to the giveaway folder.")]
    [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
    public async Task AddGiveawayAttachAsync()
    {
        await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
        await UploadGiveawayPokemonFile(Info.Hub.Config.Folder.GiveAwayFolder).ConfigureAwait(false);
    }

    [Command("AddGiveawayPokemon")]
    [Alias("addgap", "agap")]
    [Summary("Adds Pokémon to the giveaway folder based on supplied showdown set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
    public async Task AddGiveawayAsync([Summary("Showdown Set")][Remainder] string content)
    {
        Match match = Regex.Match(content, @"\""([^\""]+)\""");

        if (match.Success)
        {
            string filename = match.Groups[1].Value;
            string trimmed = content[(match.Index + match.Length)..].Trim();

            trimmed = ReusableActions.StripCodeBlock(trimmed);
            var set = new ShowdownSet(trimmed);
            var template = AutoLegalityWrapper.GetTemplate(set);
            if (set.InvalidLines.Count != 0)
            {
                var msg = LanguageHelper.ParseSet(Lang) + $"\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            try
            {
                var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
                var sav = SaveUtil.GetBlankSAV(trainer.Version, trainer.OT);
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                if (pkm is not PB7)
                {
                    pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                }
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
                await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
                await UploadGiveawayPokemonSet(Info.Hub.Config.Folder.GiveAwayFolder, filename, pk).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                if (set != null)
                {
                    LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                    var msg = LanguageHelper.Unexpected(Lang) + $"\n```{string.Join("\n", set.GetSetLines())}```";
                    await ReplyAsync(msg).ConfigureAwait(false);
                }
                else
                {
                    LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                    var msg = LanguageHelper.Unexpected(Lang);
                    await ReplyAsync(msg).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task UploadGiveawayPokemonSet(string folder, string fileName, T pk)
    {
        if (!Directory.Exists(folder))
            return;
        Directory.CreateDirectory(folder);
        var fn = Path.Combine(folder, fileName + Path.GetExtension(pk.FileName));
        File.WriteAllBytes(fn, pk.DecryptedPartyData);
        LogUtil.LogInfo($"Saved file: {fn}", $"{folder}");
        await ReplyAsync($"{Format.Bold(fileName)} added to the giveaway folder.");
        var pool = Info.Hub.LedyGA.Pool.Reload(Info.Hub.Config.Folder.GiveAwayFolder);
        if (!pool)
            LogUtil.LogInfo("Error", "Failed to reload from folder.");
    }

    public async Task UploadGiveawayPokemonFile(string folder)
    {
        if (!Directory.Exists(folder))
        {
            await ReplyAsync($"No giveaway folder found.");
            return;
        }
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == default)
        {
            await ReplyAsync("No attachment provided!").ConfigureAwait(false);
            return;
        }

        var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }
        Directory.CreateDirectory(folder);
        var gaName = attachment.Filename.Replace("_", " ");
        var fn = Path.Combine(folder, gaName);
        File.WriteAllBytes(fn, pk.DecryptedPartyData);
        LogUtil.LogInfo($"Saved file: {fn}", $"{folder}");
        await ReplyAsync($"{Format.Bold(gaName[..^4])} added to the {folder} folder.");
        var pool = Info.Hub.LedyGA.Pool.Reload(Info.Hub.Config.Folder.GiveAwayFolder);
        if (!pool)
            LogUtil.LogInfo("Error", "Failed to reload from folder.");
    }

    private static T? GetRequest(Download<PKM> dl)
    {
        if (!dl.Success)
            return null;
        return dl.Data switch
        {
            null => null,
            T pk => pk,
            _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
        };

    }
}