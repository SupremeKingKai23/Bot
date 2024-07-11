using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public partial class AnnouncementModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

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

        string imagePath = "";

        if (Context.Message.Attachments.Count > 0)
        {
            var atch = Context.Message.Attachments.FirstOrDefault();
            if (atch != null)
            {
                using var httpClient = new HttpClient();
                imagePath = Path.Combine(Path.GetTempPath(), atch.Filename);
                using var response = await httpClient.GetAsync(atch.Url);
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(imagePath, FileMode.Create);
                await stream.CopyToAsync(fileStream);
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
                            if (Hub.Config.Discord.UseThumbnailImageForAnnouncement)
                                embed.WithThumbnailUrl($"attachment://{Path.GetFileName(imagePath)}");
                            else embed.WithImageUrl($"attachment://{Path.GetFileName(imagePath)}");

                            await channel.SendFileAsync(imagePath, embed: embed.Build()).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            LogUtil.LogSafe(ex, $"Failed to send announcement message in {channel.Name} of {guild.Name}: {ex.Message}");
                        }
                    }
                }
            }
            if (File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }
        }
    }

    [Command("AnnouncementTest")]
    [Alias("testannounce", "testmsg")]
    [Summary("Sends a test announcement message to the current channel.")]
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
        if (Hub.Config.Discord.UseThumbnailImageForAnnouncement)
            embed.WithThumbnailUrl(ImgURL);
        else embed.WithImageUrl(ImgURL);
        await Context.Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
}