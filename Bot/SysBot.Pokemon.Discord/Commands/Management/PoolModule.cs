using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Distribution Pool Module")]
public class PoolModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("DistributionPoolReload")]
    [Alias("PoolReload", "LedyPoolRelooad", "dpr", "lpr")]
    [Summary("Reloads the bot pool from the setting's folder.")]
    [RequireSudo]
    public async Task ReloadPoolAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;

        var pool = hub.Ledy.Pool.Reload(hub.Config.Folder.DistributeFolder);
        if (!pool)
            await ReplyAsync("Failed to reload from Distribution folder.").ConfigureAwait(false);
        else
            await ReplyAsync($"Reloaded from Distribution folder. Pool count: {hub.Ledy.Pool.Count}").ConfigureAwait(false);
    }

    [Command("SurprisePoolReload")]
    [Alias("stpr", "spr")]
    [Summary("Reloads the bot pool from the setting's folder.")]
    [RequireSudo]
    public async Task SurpriseReloadPoolAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;

        var pool = hub.LedyST.Pool.Reload(hub.Config.Folder.SurpriseTradeFolder);
        if (!pool)
            await ReplyAsync("Failed to reload from Surprise folder.").ConfigureAwait(false);
        else
            await ReplyAsync($"Reloaded from Surprise folder. Surprise Pool count: {hub.LedyST.Pool.Count}").ConfigureAwait(false);
    }

    [Command("LedyPool")]
    [Alias("pool", "lpool", "lp")]
    [Summary("Displays the details of Pokémon files in the random pool.")]
    public async Task DisplayPoolCountAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var pool = hub.Ledy.Pool;
        var count = pool.Count;
        if (count is > 0 and < 20)
        {
            var lines = pool.Files.Select((z, i) => $"{i + 1:00}: {z.Key} = {(Species)z.Value.RequestInfo.Species}");
            var msg = string.Join("\n", lines);

            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = $"Count: {count}";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("Pool Details", embed: embed.Build()).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync($"Distribution/Ledy Pool Count: {count}").ConfigureAwait(false);
        }
    }

    [Command("SurprisePool")]
    [Alias("stpool", "spool", "stp")]
    [Summary("Displays the details of Pokémon files in the Surprise trade pool.")]
    public async Task DisplaySTPoolCountAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var pool = hub.LedyST.Pool;
        var count = pool.Count;
        if (count is > 0 and < 20)
        {
            var lines = pool.Files.Select((z, i) => $"{i + 1:00}: {z.Key} = {(Species)z.Value.RequestInfo.Species}");
            var msg = string.Join("\n", lines);

            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = $"Count: {count}";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("Surprise Pool Details", embed: embed.Build()).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync($"Surprise Pool Count: {count}").ConfigureAwait(false);
        }
    }
}