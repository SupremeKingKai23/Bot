using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Data;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class OwnerModule<T> : SudoModule<T> where T : PKM, new()
    {
        private readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

        [Command("addSudo")]
        [Summary("Adds mentioned user to global sudo")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task SudoUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("removeSudo")]
        [Summary("Removes mentioned user from global sudo")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveSudoUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("addChannel")]
        [Summary("Adds a channel to the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task AddChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.AddIfNew(new[] { obj });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }
              
        [Command("removeChannel")]
        [Summary("Removes a channel from the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("addMonitorChannel")]
        [Alias("monitorchannel", "amc")]
        [Summary("Adds a channel to the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task AddMonitorChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.BotStatus.MonitoredChannels.AddIfNew(new[] { obj });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("removeMonitorChannel")]
        [Alias("removemonitor", "rmc")]
        [Summary("Adds a channel to the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveMonitorChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.BotStatus.MonitoredChannels.RemoveAll(z => z.ID == obj.ID);
            await ReplyAsync("Done.").ConfigureAwait(false);

        }

        [Command("addtraderole")]
        [Alias("addrole")]
        [Summary("Adds the mentioned Role to the RoleCanTrade list.")]
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

        [Command("removetraderole")]
        [Alias("removerole")]
        [Summary("Removes the mentioned Role to the RoleCanTrade list")]
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

        [Command("addfavoredrole")]
        [Alias("addfr")]
        [Summary("Adds the mentioned Role to the RoleCanTrade list.")]
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

        [Command("removefavoredrole")]
        [Alias("removefr")]
        [Summary("Removes the mentioned Role to the RoleCanTrade list")]
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

        [Command("leave")]
        [Alias("bye")]
        [Summary("Leaves the server the command is issued in.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task Leave()
        {
            await ReplyAsync("Goodbye.").ConfigureAwait(false);
            await Context.Guild.LeaveAsync().ConfigureAwait(false);
        }

        [Command("leaveguild")]
        [Alias("lg")]
        [Summary("Leaves guild based on supplied ID.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
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

        [Command("leaveall")]
        [Summary("Leaves all servers the bot is currently in.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task LeaveAll()
        {
            await ReplyAsync("Leaving all servers.").ConfigureAwait(false);
            foreach (var guild in Context.Client.Guilds)
            {
                await guild.LeaveAsync().ConfigureAwait(false);
            }
        }

        [Command("listguilds")]
        [Alias("guildlist", "gl")]
        [Summary("Lists all the servers the bot is in.")]
        [RequireOwner]
        public async Task ListGuilds()
        {
            var guilds = Context.Client.Guilds.OrderBy(guild => guild.Name);

            var embed = new EmbedBuilder
            {
                Title = "Guild List",
                Description = "Here is a list of all servers this bot is currently in"
            };

            foreach (var guild in guilds)
            {
                embed.AddField($"**{guild.Name}**", $"ID: {guild.Id}");
            }

            await ReplyAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("Announcement")]
        [Alias("announce")]
        [Summary("Sends an announcement message to all guild channels the bot is in.")]
        [RequireOwner]
        public async Task AnnouncementBroadcast([Summary("Message to Broadcast")][Remainder] string message = "")
        {
            if (Context.Message.Attachments.Count == 0 && string.IsNullOrWhiteSpace(message))
            {
                await ReplyAsync("You must include a message or attach an image.").ConfigureAwait(false);
                return;
            }

            await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
            var ImgURL = Hub.Config.Discord.AnnouncementImgURL;
            if (Context.Message.Attachments.Count > 0)
            {
                var atch = Context.Message.Attachments.FirstOrDefault();
                if (atch != null)
                ImgURL = atch.Url;
            }

            var guilds = Context.Client.Guilds;
            foreach (var guild in guilds)
            {
                var channels = guild.TextChannels;
                foreach (var channel in channels)
                {
                    if (Hub.Config.Discord.ChannelWhitelist.Contains(channel.Id))
                    {
                        try
                        {
                            var embed = new EmbedBuilder();
                            embed.WithColor(Color.Green);
                            embed.WithDescription($"# Announcement\n{(message != "" ? $"### {message}" : "")}");
                            embed.WithImageUrl(ImgURL);
                            await channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            LogUtil.LogSafe(ex, $"Failed to send announcement message in {channel.Name} of {guild.Name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        [Command("AnnouncementTest")]
        [Alias("testannounce", "testmsg")]
        [Summary("Sends an announcement message to all guild channels the bot is in.")]
        [RequireOwner]
        public async Task AnnouncementTestBroadcast([Summary("Message to Broadcast")][Remainder] string message = "")
        {
            if (Context.Message.Attachments.Count == 0 && string.IsNullOrWhiteSpace(message))
            {
                await ReplyAsync("You must include a message or attach an image.").ConfigureAwait(false);
                return;
            }

            await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
            var ImgURL = Hub.Config.Discord.AnnouncementImgURL;
            if (Context.Message.Attachments.Count > 0)
            {
                var atch = Context.Message.Attachments.FirstOrDefault();
                if (atch != null)
                    ImgURL = atch.Url;
            }
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithDescription($"# Announcement\n{(message != "" ? $"### {message}" : "")}");
            embed.WithImageUrl(ImgURL);
            await Context.Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("sudoku")]
        [Alias("kill", "shutdown")]
        [Summary("Causes the entire process to end itself!")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
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

        private RemoteControlAccess GetRoleReference(SocketRole role) => new()
        {
            ID = role.Id,
            Name = $"{role.Name}",
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

    }
}
