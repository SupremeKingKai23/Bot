﻿using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Clone trades")]
public class CloneModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("Clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public Task CloneAsync([Summary("Trade Code")] int code)
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
        return QueueHelper<T>.AddToQueueAsync(Context, tcode, sig, new T(), PokeRoutineType.Clone, PokeTradeType.Clone, Context.User, lgcode, true);
    }

    [Command("Clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public Task CloneAsync()
    {
        if (typeof(T) != typeof(PB7))
        {
            var code = Info.GetRandomTradeCode();
            return CloneAsync(code);
        }
        else
        {
            var code = Info.GetRandomLGPENumCode();
            return CloneAsync(code == 0 ? 000 : code);
        }
    }

    [Command("CloneList")]
    [Alias("cl", "cq")]
    [Summary("Prints the users in the Clone queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Clone);
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