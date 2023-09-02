using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public partial class HelloModule : ModuleBase<SocketCommandContext>
    {
        private bool HasURL;

        [Command("hello")]
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
            var imageExtensions = new string[] { ".png", ".jpg", ".jpeg", ".gif" };
            return imageExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        [GeneratedRegex("(http|https)://[^\\s]+")]
        private static partial Regex urls();
    }
}