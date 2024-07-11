using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Clears and toggles Queue features.")]
public class QueueModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private static BotLanguage Lang => Info.Hub.Config.CurrentLanguage;

    [Command("QueueStatus")]
    [Alias("qs", "ts")]
    [Summary("Checks the user's position in the queue.")]
    public async Task GetTradePositionAsync()
    {
        var msg = Context.User.Mention + " - " + Info.GetPositionString(Context.User.Id);
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    [Command("QueueClear")]
    [Alias("qc", "tc")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    public async Task ClearTradeAsync()
    {
        string msg = ClearTrade();
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    [Command("QueueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync([Summary("Discord user ID")] ulong id)
    {
        string msg = ClearTrade(id);
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    [Command("QueueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync([Summary("Username of the person to clear")] string _)
    {
        foreach (var user in Context.Message.MentionedUsers)
        {
            string msg = ClearTrade(user.Id);
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }

    [Command("QueueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync()
    {
        var users = Context.Message.MentionedUsers;
        if (users.Count == 0)
        {
            await ReplyAsync("No users mentioned").ConfigureAwait(false);
            return;
        }
        foreach (var u in users)
            await ClearTradeUserAsync(u.Id).ConfigureAwait(false);
    }

    [Command("QueueClearAll")]
    [Alias("qca", "tca")]
    [Summary("Clears all users from the trade queues.")]
    [RequireSudo]
    public async Task ClearAllTradesAsync()
    {
        Info.ClearAllQueues();
        await ReplyAsync("Cleared all in the queue.").ConfigureAwait(false);
    }

    [Command("QueueToggle")]
    [Alias("qt", "tt")]
    [Summary("Toggles on/off the ability to join the trade queue.")]
    [RequireSudo]
    public async Task ToggleQueueTradeAsync()
    {
        var state = Info.ToggleQueue();
        var msg = state
            ? "Users are now able to join the trade queue."
            : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";

        await Context.Channel.EchoAndReply(msg).ConfigureAwait(false);
    }

    [Command("QueueMode")]
    [Alias("qm")]
    [Summary("Changes how queueing is controlled (manual/threshold/interval).")]
    [RequireSudo]
    public async Task ChangeQueueModeAsync([Summary("Queue mode")] QueueOpening mode)
    {
        SysCord<T>.Runner.Hub.Config.Queues.QueueToggleMode = mode;
        await ReplyAsync($"Changed queue mode to {mode}.").ConfigureAwait(false);
    }

    [Command("QueueList")]
    [Alias("ql")]
    [Summary("Private messages the list of users in the queue.")]
    [RequireSudo]
    public async Task ListUserQueue()
    {
        var lines = SysCord<T>.Runner.Hub.Queues.Info.GetUserList("(ID {0}) - Code: {1} - {2} - {3}");
        var msg = string.Join("\n", lines);
        if (msg.Length < 3)
            await ReplyAsync("Queue list is empty.").ConfigureAwait(false);
        else
            await Context.User.SendMessageAsync(msg).ConfigureAwait(false);
    }

    private string ClearTrade()
    {
        var userID = Context.User.Id;
        return ClearTrade(userID);
    }

    private static string ClearTrade(ulong userID)
    {
        var result = Info.ClearTrade(userID);
        return GetClearTradeMessage(result);
    }

    private static string GetClearTradeMessage(QueueResultRemove result)
    {
        return result switch
        {
            QueueResultRemove.CurrentlyProcessing => LanguageHelper.QueueCP(Lang),
            QueueResultRemove.CurrentlyProcessingRemoved => LanguageHelper.QueueCPRemoved(Lang),
            QueueResultRemove.Removed => LanguageHelper.QueueRemoved(Lang),
            _ => LanguageHelper.NotInQueue(Lang)
        };
    }
}