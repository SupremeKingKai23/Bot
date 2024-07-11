using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Dump trades")]
public class DumpModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("Dump")]
    [Alias("d")]
    [Summary("Dumps the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task DumpAsync([Summary("Trade Code")] int code)
    {
        List<PictoCodes>? lgcode;
        int tcode;
        if (typeof(T) == typeof(PB7))
        {
            lgcode = ReusableActions.GetLGPETradeCode(code);
            tcode = Info.GetRandomTradeCode();
        }
        else
        {
            lgcode = Info.GetRandomLGTradeCode();
            tcode = code;
        }
        var sig = Context.User.GetFavor();
        return QueueHelper<T>.AddToQueueAsync(Context, tcode, sig, new T(), PokeRoutineType.Dump, PokeTradeType.Dump, Context.User, lgcode, true);
    }

    [Command("Dump")]
    [Alias("d")]
    [Summary("Dumps the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task DumpAsync()
    {
        if (typeof(T) != typeof(PB7))
        {
            var code = Info.GetRandomTradeCode();
            return DumpAsync(code);
        }
        else
        {
            var code = Info.GetRandomLGPENumCode();
            return DumpAsync(code == 0 ? 000 : code);
        }
    }

    [Command("DumpList")]
    [Alias("dl", "dq")]
    [Summary("Prints the users in the Dump queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Dump);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }
}