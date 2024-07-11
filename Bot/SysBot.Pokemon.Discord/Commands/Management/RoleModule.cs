using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class RoleModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

    [Command("AddTradeRole")]
    [Alias("art")]
    [Summary("Adds the mentioned Role to the \"RoleCanTrade\" list.")]
    [RequireOwner]
    public async Task AddTradeRole([Summary("Mentioned Role")] SocketRole role)
    {
        if (Hub.Config.Discord.RoleCanTrade.Contains(role.Id))
        {
            await ReplyAsync("Role Already exists in settings.").ConfigureAwait(false);
            return;
        }

        SysCordSettings.Settings.RoleCanTrade.AddIfNew(new[] { GetRoleReference(role) });
        await ReplyAsync($"Added {role.Name} to the list!").ConfigureAwait(false);
    }

    [Command("RemoveTradeRole")]
    [Alias("rrt")]
    [Summary("Removes the mentioned Role from the \"RoleCanTrade\" list")]
    [RequireOwner]
    public async Task RemoveTradeRole([Summary("Mentioned Role")] SocketRole role)
    {
        if (!Hub.Config.Discord.RoleCanTrade.Contains(role.Id))
        {
            await ReplyAsync("Role does not exists in settings.").ConfigureAwait(false);
            return;
        }
        SysCordSettings.Settings.RoleCanTrade.RemoveAll(z => z.ID == role.Id);
        await ReplyAsync($"Removed {role.Name} from the list.").ConfigureAwait(false);
    }

    [Command("AddcloneRole")]
    [Alias("arc")]
    [Summary("Adds the mentioned Role to the \"RoleCanClone\" list.")]
    [RequireOwner]
    public async Task AddCloneRole([Summary("Mentioned Role")] SocketRole role)
    {
        if (Hub.Config.Discord.RoleCanClone.Contains(role.Id))
        {
            await ReplyAsync("Role Already exists in settings.").ConfigureAwait(false);
            return;
        }

        SysCordSettings.Settings.RoleCanClone.AddIfNew(new[] { GetRoleReference(role) });
        await ReplyAsync($"Added {role.Name} to the list!").ConfigureAwait(false);
    }

    [Command("RemoveCloneRole")]
    [Alias("rrc")]
    [Summary("Removes the mentioned Role from the \"RoleCanClone\" list")]
    [RequireOwner]
    public async Task RemoveCloneRole([Summary("Mentioned Role")] SocketRole role)
    {
        if (!Hub.Config.Discord.RoleCanClone.Contains(role.Id))
        {
            await ReplyAsync("Role does not exists in settings.").ConfigureAwait(false);
            return;
        }
        SysCordSettings.Settings.RoleCanClone.RemoveAll(z => z.ID == role.Id);
        await ReplyAsync($"Removed {role.Name} from the list.").ConfigureAwait(false);
    }

    [Command("AddFavoredRole")]
    [Alias("addfr")]
    [Summary("Adds the mentioned Role to the \"RoleCanTrade\" list.")]
    [RequireOwner]
    public async Task AddFavoredRole([Summary("Mentioned Role")] SocketRole role)
    {
        if (Hub.Config.Discord.RoleFavored.Contains(role.Id))
        {
            await ReplyAsync("Role Already exists in settings.").ConfigureAwait(false);
            return;
        }

        SysCordSettings.Settings.RoleFavored.AddIfNew(new[] { GetRoleReference(role) });
        await ReplyAsync($"Added {role.Name} to the list!").ConfigureAwait(false);
    }

    [Command("RemoveFavoredRole")]
    [Alias("removefr")]
    [Summary("Removes the mentioned Role from the \"RoleCanTrade\" list")]
    [RequireOwner]
    public async Task RemoveFavoredRole([Summary("Mentioned Role")] SocketRole role)
    {
        if (!Hub.Config.Discord.RoleFavored.Contains(role.Id))
        {
            await ReplyAsync("Role does not exists in settings.").ConfigureAwait(false);
            return;
        }
        SysCordSettings.Settings.RoleFavored.RemoveAll(z => z.ID == role.Id);
        await ReplyAsync($"Removed {role.Name} from the list.").ConfigureAwait(false);
    }

    private RemoteControlAccess GetRoleReference(SocketRole role) => new()
    {
        ID = role.Id,
        Name = $"{role.Name}",
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}