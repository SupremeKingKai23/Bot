using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class SudoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private readonly ExtraCommandUtil<T> Util = new();
    private readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

    [Command("Blacklist")]
    [Summary("Blacklists a mentioned Discord user.")]
    [RequireSudo]
    // ReSharper disable once UnusedParameter.Global
    public async Task BlackListUsers([Summary("Mentioned Users")][Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("BlacklistComment")]
    [Summary("Adds a comment for a blacklisted Discord user ID.")]
    [RequireSudo]
    // ReSharper disable once UnusedParameter.Global
    public async Task BlackListUsers([Summary("Discord ID")] ulong id, [Summary("Comment")][Remainder] string comment)
    {
        var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await ReplyAsync($"Unable to find a user with that ID ({id}).").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
    }

    [Command("Unblacklist")]
    [Summary("Removes a mentioned Discord user from the blacklist.")]
    [RequireSudo]
    // ReSharper disable once UnusedParameter.Global
    public async Task UnBlackListUsers([Summary("Mentioned Users")][Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => objects.Any(o => o.ID == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("BlacklistID")]
    [Summary("Blacklists Discord user IDs. (Useful if user is not in the server).")]
    [RequireSudo]
    public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("UnBlacklistID")]
    [Summary("Removes Discord user IDs from the blacklist. (Useful if user is not in the server).")]
    [RequireSudo]
    public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => IDs.Any(o => o == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("BlacklistSummary")]
    [Alias("printBlacklist", "blacklistPrint")]
    [Summary("Prints the list of blacklisted Discord users.")]
    [RequireSudo]
    public async Task PrintBlacklist()
    {
        var lines = SysCordSettings.Settings.UserBlacklist.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [Command("BanNID")]
    [Alias("bt")]
    [Summary("Bans user with provided NID from trading with the bot")]
    [RequireSudo]
    public async Task BanTradeAsync([Summary("Online ID")] ulong nid, [Summary("Comment")] string comment)
    {
        SysCordSettings.HubConfig.TradeAbuse.BannedIDs.AddIfNew([GetReference(nid, comment)]);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("BanID")]
    [Summary("Bans online user IDs.")]
    [RequireSudo]
    public async Task BanOnlineIDs([Summary("Comma Separated Online IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);

        Hub.Config.TradeAbuse.BannedIDs.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("BannedIDComment")]
    [Summary("Adds a comment for a banned online user ID.")]
    [RequireSudo]
    public async Task BanOnlineIDComment(ulong id, [Remainder] string comment)
    {
        var obj = Hub.Config.TradeAbuse.BannedIDs.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await ReplyAsync($"Unable to find a user with that online ID ({id}).").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
    }

    [Command("UnbanID")]
    [Summary("Bans online user IDs.")]
    [RequireSudo]
    public async Task UnBanOnlineIDs([Summary("Comma Separated Online IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);

        Hub.Config.TradeAbuse.BannedIDs.RemoveAll(z => IDs.Any(o => o == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("BannedIDSummary")]
    [Alias("printBannedID", "bannedIDPrint", "IDSum")]
    [Summary("Prints the list of banned online IDs.")]
    [RequireSudo]
    public async Task PrintBannedOnlineIDs()
    {
        var lines = Hub.Config.TradeAbuse.BannedIDs.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [Command("ForgetUser")]
    [Alias("forget")]
    [Summary("Forgets users that were previously encountered.")]
    [RequireSudo]
    public async Task ForgetPreviousUser([Summary("Comma Separated Online IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);

        foreach (var ID in IDs)
        {
            var removedUser = PokeRoutineExecutorBase.PreviousUsers.TryGetPreviousNID(ID);
            if (removedUser != null)
            {
                PokeRoutineExecutorBase.PreviousUsers.RemoveAllNID(ID);
                PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAllNID(ID);
                await ReplyAsync($"Removed user {removedUser.Name}, NID: {ID}.").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync("No user with that NID found in the list.").ConfigureAwait(false);
            }
        }
    }

    [Command("ForgetUser")]
    [Alias("forget")]
    [Summary("Forgets mentioned users trade information by IGN.")]
    [RequireSudo]
    public async Task ForgetPreviousUserName([Summary("User IGN")][Remainder] string content)
    {
        var removedUser = PokeRoutineExecutorBase.PreviousUsers.TryGetPreviousName(content);

        if (removedUser != null)
        {
            PokeRoutineExecutorBase.PreviousUsers.RemoveAllName(content);
            PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAllName(content);

            await ReplyAsync($"Removed user {content}.").ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync($"User {content} not found in the list.").ConfigureAwait(false);
        }
    }

    [Command("ForgetUser")]
    [Alias("forget")]
    [Summary("Forgets mentioned users trade information.")]
    [RequireSudo]
    public async Task ForgetPreviousUserMention([Summary("Mentioned User")][Remainder] SocketUser user)
    {
        var ID = user.Id;

        var removedUser = PokeRoutineExecutorBase.PreviousUsers.TryGetPreviousRemoteID(ID);

        if (removedUser != null)
        {
            PokeRoutineExecutorBase.PreviousUsers.RemoveAllRemoteID(ID);
            PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAllRemoteID(ID);

            await ReplyAsync($"Removed user {user.Username}.").ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync("User not found in the list.").ConfigureAwait(false);
        }
    }

    [Command("PreviousUserSummary")]
    [Alias("prevUsers")]
    [Summary("Prints a list of previously encountered users.")]
    [RequireSudo]
    public async Task PrintPreviousUsers()
    {
        var previousUsers = PokeRoutineExecutorBase.PreviousUsers.Summarize();
        var previousDistributionUsers = PokeRoutineExecutorBase.PreviousUsersDistribution.Summarize();

        if (previousUsers.Any())
        {
            await Util.ListUtil(Context, "Previous Users:", string.Join("\n", previousUsers)).ConfigureAwait(false);
        }

        if (previousDistributionUsers.Any())
        {
            await Util.ListUtil(Context, "Previous Distribution Users:", string.Join("\n", previousDistributionUsers)).ConfigureAwait(false);
        }

        if (!previousUsers.Any() && !previousDistributionUsers.Any())
            await ReplyAsync("No previous users found.").ConfigureAwait(false);
    }

    [Command("LGPEUserSummary")]
    [Alias("LGPEUsers")]
    [Summary("Prints a list of previously encountered users.")]
    [RequireSudo]
    public async Task PrintPreviousLGPEUsers()
    {
        var previousUsers = PokeRoutineExecutorBase.LGPEOTInfo.Summarize();

        if (previousUsers.Any())
        {
            await Util.ListUtil(Context, "Previous Users:", string.Join("\n", previousUsers)).ConfigureAwait(false);
        }

        if (!previousUsers.Any())
            await ReplyAsync("No previous users found.").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(IUser usr) => new()
    {
        ID = usr.Id,
        Name = usr.Username,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(ulong id) => new()
    {
        ID = id,
        Name = "Manual",
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(ulong id, string comment) => new()
    {
        ID = id,
        Name = id.ToString(),
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
    };

    protected static IEnumerable<ulong> GetIDs(string content)
    {
        return content.Split([",", ", ", " "], StringSplitOptions.RemoveEmptyEntries)
            .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
    }
}