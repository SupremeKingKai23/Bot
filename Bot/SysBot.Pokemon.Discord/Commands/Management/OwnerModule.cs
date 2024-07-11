using Discord;
using Discord.Commands;
using PKHeX.Core;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class OwnerModule<T> : SudoModule<T> where T : PKM, new()
{
    private readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;
    private readonly ExtraCommandUtil<T> Util = new();

    [Command("AddSudo")]
    [Summary("Adds mentioned user to global sudo")]
    [RequireOwner]
    public async Task SudoUsers([Summary("Mentioned Users")][Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("UpdateLanguage")]
    [Alias("ul")]
    [Summary("Updates the user communications language")]
    [RequireOwner]
    public async Task UpdateLanguage([Summary("Language")][Remainder] string lang)
    {
        var lang2 = lang.ToLower() is "espanol" or "spanish" or "español" or "2" ? Pokemon.Helpers.BotLanguage.Español : lang.ToLower() is "italian" or "italiano" or "italiana" or "3" ? Pokemon.Helpers.BotLanguage.Italiano : lang.ToLower() is "german" or "deutsch" or "4" ? Pokemon.Helpers.BotLanguage.Deutsch : lang.ToLower() is "french" or "francais" or "français" or "5" ? Pokemon.Helpers.BotLanguage.Français : Pokemon.Helpers.BotLanguage.English;
        Hub.Config.CurrentLanguage = lang2;
        await ReplyAsync($"Changed Language to **{lang2}**.").ConfigureAwait(false);
    }

    [Command("RemoveSudo")]
    [Summary("Removes mentioned user from global sudo")]
    [RequireOwner]
    public async Task RemoveSudoUsers([Summary("Mentioned Users")][Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("AddChannel")]
    [Summary("Adds current channel to the list of channels that are accepting commands.")]
    [RequireOwner]
    public async Task AddChannel()
    {
        if (SysCordSettings.Settings.ChannelWhitelist.Contains(Context.Channel.Id))
            await ReplyAsync("Channel already added").ConfigureAwait(false);
        else
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.AddIfNew(new[] { obj });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }
    }

    [Command("RemoveChannel")]
    [Summary("Removes current channel from the list of channels that are accepting commands.")]
    [RequireOwner]
    public async Task RemoveChannel()
    {
        if (!SysCordSettings.Settings.ChannelWhitelist.Contains(Context.Channel.Id))
            await ReplyAsync("Channel not found").ConfigureAwait(false);
        else
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }
    }

    [Command("ListWhitelistedChannels")]
    [Alias("listchannels", "cwl", "wlc")]
    [Summary("Lists all channels that are whitelisted for bot operation.")]
    [RequireOwner]
    public async Task ListWhitelistedChannels()
    {
        var whitelist = SysCordSettings.Settings.ChannelWhitelist.List;
        var guilds = Context.Client.Guilds.OrderBy(guild => guild.Name);
        var guildList = new StringBuilder();
        guildList.AppendLine("\n");
        foreach (var guild in guilds)
        {
            var guildChannels = whitelist
                .Where(channel => guild.GetChannel(channel.ID) != null)
                .OrderBy(channel => guild.GetChannel(channel.ID).Name)
                .ToList();

            if (guildChannels.Count > 0)
            {
                var channelNames = string.Join("\n", guildChannels.Select(channel => $"<#{channel.ID}>"));
                if (!string.IsNullOrWhiteSpace(channelNames))
                {
                    guildList.AppendLine($"{Format.Underline(Format.Bold($"{guild.Name}"))}\n{channelNames}\n");
                }
            }
            else
            {
                guildList.AppendLine($"{Format.Underline(Format.Bold($"{guild.Name}"))}\nNo whitelisted channels in this guild.\n");
            }
        }
        await Util.ListUtil(Context, "Here is a list of currently whitelisted Channels by Server", guildList.ToString()).ConfigureAwait(false);
    }

    [Command("AddMonitorChannel")]
    [Alias("monitorchannel", "amc")]
    [Summary("Adds current channel to the list of channels that are used for Bot Status updates")]
    [RequireOwner]
    public async Task AddMonitorChannel()
    {
        if (SysCordSettings.Settings.BotStatus.MonitoredChannels.Contains(Context.Channel.Id))
            await ReplyAsync("Channel already added.").ConfigureAwait(false);
        else
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.BotStatus.MonitoredChannels.AddIfNew(new[] { obj });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }
    }

    [Command("RemoveMonitorChannel")]
    [Alias("removemonitor", "rmc")]
    [Summary("Removes a channel from the list of channels that are used for Bot Status updates.")]
    [RequireOwner]
    public async Task RemoveMonitorChannel()
    {
        if (!SysCordSettings.Settings.BotStatus.MonitoredChannels.Contains(Context.Channel.Id))
            await ReplyAsync("Channel not found.").ConfigureAwait(false);
        else
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.BotStatus.MonitoredChannels.RemoveAll(z => z.ID == obj.ID);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }
    }

    [Command("Leave")]
    [Alias("bye")]
    [Summary("Leaves the server the command is issued in.")]
    [RequireOwner]
    public async Task Leave()
    {
        await ReplyAsync("Goodbye.").ConfigureAwait(false);
        await Context.Guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("LeaveGuild")]
    [Alias("lg")]
    [Summary("Leaves guild based on supplied ID.")]
    [RequireOwner]
    public async Task LeaveGuild(string userInput)
    {
        if (!ulong.TryParse(userInput, out ulong id))
        {
            await ReplyAsync("Please provide a valid Guild ID.").ConfigureAwait(false);
            return;
        }

        var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            await ReplyAsync($"Provided input ({userInput}) is not a valid guild ID or the bot is not in the specified guild.").ConfigureAwait(false);
            return;
        }

        await ReplyAsync($"Leaving {guild}.").ConfigureAwait(false);
        await guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("LeaveAll")]
    [Summary("Leaves all servers the bot is currently in.")]
    [RequireOwner]
    public async Task LeaveAll()
    {
        await ReplyAsync("Leaving all servers.").ConfigureAwait(false);
        foreach (var guild in Context.Client.Guilds)
        {
            await guild.LeaveAsync().ConfigureAwait(false);
        }
    }

    [Command("ListGuilds")]
    [Alias("guildlist", "gl")]
    [Summary("Lists all the servers the bot is in.")]
    [RequireOwner]
    public async Task ListGuilds()
    {
        var guilds = Context.Client.Guilds.OrderBy(guild => guild.Name);
        var guildList = new StringBuilder();
        guildList.AppendLine("\n");
        foreach (var guild in guilds)
        {
            guildList.AppendLine($"{Format.Bold($"{guild.Name}")}\nID: {guild.Id}\n");
        }
        await Util.ListUtil(Context, "Here is a list of all servers this bot is currently in", guildList.ToString()).ConfigureAwait(false);
    }

    [Command("ToggleEmbeds")]
    [Alias("te")]
    [Summary("Toggles the 'UseTradeEmbeds' setting.")]
    [RequireOwner]
    public async Task ToggleEmbeds()
    {
        Hub.Config.Trade.EmbedSettings.UseTradeEmbeds = !Hub.Config.Trade.EmbedSettings.UseTradeEmbeds;
        await ReplyAsync($"UseTradeEmbeds set to: {Hub.Config.Trade.EmbedSettings.UseTradeEmbeds}");
    }

    [Command("ToggleAutoOT")]
    [Alias("taot", "tot")]
    [Summary("Toggles the 'UseTradeTradePartnerInfo' setting.")]
    [RequireOwner]
    public async Task ToggleAutoOT()
    {
        Hub.Config.Legality.UseTradePartnerInfo = !Hub.Config.Legality.UseTradePartnerInfo;
        await ReplyAsync($"UseTradePartnerInfo set to: {Hub.Config.Legality.UseTradePartnerInfo}");
    }

    [Command("Sudoku")]
    [Alias("kill", "shutdown")]
    [Summary("Causes the entire process to end itself!")]
    [RequireOwner]
    public async Task ExitProgram()
    {
        await Context.Channel.EchoAndReply("Shutting down... goodbye! **Bot services are going offline.**").ConfigureAwait(false);
        Environment.Exit(0);
    }

    private RemoteControlAccess GetReference(IUser user) => new()
    {
        ID = user.Id,
        Name = user.Username,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}