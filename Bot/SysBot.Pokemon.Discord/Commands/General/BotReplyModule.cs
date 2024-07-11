using Discord;
using Discord.Commands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public partial class BotReplyModule : ModuleBase<SocketCommandContext>
{
    private bool HasURL;

    [Command("Hello")]
    [Alias("hi")]
    [Summary("Say hello to the bot and get a response. You can include an Image url and the bot will include it in an Embed. Supports .png, .jpg, .jpeg, and .gif")]
    public async Task HelloAsync()
    {
        var str = SysCordSettings.Settings.HelloResponse;
        var msg = string.Format(str, Context.User.Mention);
        var embed = CreateEmbed(msg);

        if (HasURL)
        {
            await ReplyAsync(embed: embed).ConfigureAwait(false);
        }
        else
        {
            if (SysCordSettings.Settings.HelloResponse != "")
            {
                await ReplyAsync(msg).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"Hello {Context.User.Mention}!").ConfigureAwait(false);
            }
        }
    }

    [Command("Ping")]
    [Summary("Makes the bot respond, indicating that it is running.")]
    public async Task PingAsync()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var embed = new EmbedBuilder()
            .WithTitle("Ping")
            .WithDescription("Pinging...")
            .WithColor(Color.Blue)
            .Build();

        var message = await ReplyAsync(embed: embed).ConfigureAwait(false);

        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        embed = new EmbedBuilder()
            .WithTitle("Pong!")
            .WithColor(Color.Green)
            .WithFooter($"Response Time: {responseTime}ms")
            .Build();

        await message.ModifyAsync(msg => msg.Embed = embed).ConfigureAwait(false);
    }

    [Command("ThankYou")]
    [Alias("thanks!", "thanks", "ty", "ty!")]
    [Summary("Say thanks to the bot and get a response. You can include an Image url and the bot will include it in an Embed. Supports .png, .jpg, .jpeg, and .gif")]
    public async Task ThankYouAsync()
    {
        var str = SysCordSettings.Settings.ThankYouResponse;
        var msg = string.Format(str, Context.User.Mention);
        var embed = CreateEmbed(msg);

        if (HasURL)
        {
            await ReplyAsync(embed: embed).ConfigureAwait(false);
        }
        else
        {
            if (SysCordSettings.Settings.ThankYouResponse != "")
            {
                await ReplyAsync(msg).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"You're welcome {Context.User.Mention}!").ConfigureAwait(false);
            }
        }
    }

    private Embed CreateEmbed(string message)
    {
        var embedBuilder = new EmbedBuilder();

        if (ContainsUrl(message))
        {
            HasURL = true;
            var url = ExtractUrl(message);

            if (IsImage(url))
            {
                embedBuilder.WithImageUrl(url);
            }
            if (RemoveUrl(message).Length >= 1)
            {
                embedBuilder.WithDescription($"### {RemoveUrl(message)}");
            }
        }
        else
        {
            HasURL = false;
        }

        embedBuilder.WithColor(Color.Green);

        return embedBuilder.Build();
    }

    private static Regex urlRegex = urls();

    private static bool ContainsUrl(string message)
    {
        return urlRegex.IsMatch(message);
    }

    private static string ExtractUrl(string message)
    {
        var match = urlRegex.Match(message);
        return match.Value;
    }

    private static string RemoveUrl(string message)
    {
        return urlRegex.Replace(message, "");
    }

    private static bool IsImage(string url)
    {
        string[] imageExtensions = [".png", ".jpg", ".jpeg", ".gif"];
        return imageExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex("(http|https)://[^\\s]+")]
    private static partial Regex urls();
}