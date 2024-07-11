using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class AutoModModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

    [Command("LegalityCheck")]
    [Alias("lc", "check", "validate", "verify")]
    [Summary("Verifies the attachment for legality.")]
    public async Task LegalityCheck()
    {
        var attachments = Context.Message.Attachments;
        foreach (var att in attachments)
            await LegalityCheck(att, false).ConfigureAwait(false);
    }

    [Command("LegalityCheckVerbose")]
    [Alias("lcv", "verbose")]
    [Summary("Verifies the attachment for legality with a verbose output.")]
    public async Task LegalityCheckVerbose()
    {
        var attachments = Context.Message.Attachments;
        foreach (var att in attachments)
            await LegalityCheck(att, true).ConfigureAwait(false);
    }

    [Command("Legalize"), Alias("alm")]
    [Summary("Tries to legalize the attached pkm data.")]
    public async Task LegalizeAsync()
    {
        var attachments = Context.Message.Attachments;
        foreach (var att in attachments)
            await Context.Channel.ReplyWithLegalizedSetAsync(att).ConfigureAwait(false);
    }

    [Command("Convert"), Alias("showdown")]
    [Summary("Tries to convert the Showdown Set to pkm data.")]
    [Priority(1)]
    public Task ConvertShowdown([Summary("Generation/Format")] int gen, [Remainder][Summary("Showdown Set")] string content)
    {
        return Context.Channel.ReplyWithLegalizedSetAsync(content, (byte)gen, Hub.Config.CurrentLanguage);
    }

    [Command("Convert"), Alias("showdown")]
    [Summary("Tries to convert the Showdown Set to pkm data.")]
    [Priority(0)]
    public async Task ConvertShowdown([Remainder][Summary("Showdown Set")] string content)
    {
        await Context.Channel.ReplyWithLegalizedSetAsync<T>(content, Hub.Config.CurrentLanguage).ConfigureAwait(false);
    }

    [Command("RequestShowdown")]
    [Alias("rs", "ss")]
    [Summary("Shows Showdown Format Text of attached PKM file.")]
    public async Task ShowdownRequest()
    {
        if (Context.Message.Attachments.Count > 0)
        {
            foreach (var att in Context.Message.Attachments)
                await Context.Channel.RepostPKMAsShowdownAsync(att).ConfigureAwait(false);
            return;
        }
        else
        {
            await ReplyAsync("Please attach a valid PKM file.").ConfigureAwait(false);
            return;
        }
    }

    [Command("RequestDetailedShowdown")]
    [Alias("rds")]
    [Summary("Shows detailed Showdown Format Text of attached PKM file.")]
    public async Task DetailedShowdownRequest()
    {
        if (Context.Message.Attachments.Count > 0)
        {
            foreach (var att in Context.Message.Attachments)
                await Context.Channel.RepostPKMAsShowdownAsync(att, true).ConfigureAwait(false);
            return;
        }
        else
        {
            await ReplyAsync("Please attach a valid PKM file.").ConfigureAwait(false);
            return;
        }
    }

    [Command("ReinitializeLegalitySettings")]
    [Alias("reinit", "rils")]
    [Summary("reinitializes the legality settings of the bot")]
    [RequireOwner]
    public async Task ReintializeLegality()
    {
        var message = await ReplyAsync("Re-Initializing Legality Settings...").ConfigureAwait(false);
        AutoLegalityWrapper.EnsureInitialized(Hub.Config.Legality, Hub.Config.CurrentLanguage, true);
        await message.ModifyAsync(msg => msg.Content = "Done").ConfigureAwait(false);
    }

    private async Task LegalityCheck(IAttachment att, bool verbose)
    {
        var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
        if (!download.Success)
        {
            await ReplyAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var pkm = download.Data!;
        var la = new LegalityAnalysis(pkm);
        var builder = new EmbedBuilder
        {
            Color = la.Valid ? Color.Green : Color.Red,
            Description = $"Legality Report for {download.SanitizedFileName}:",
        };

        builder.AddField(x =>
        {
            x.Name = la.Valid ? "Valid" : "Invalid";
            x.Value = la.Report(verbose);
            x.IsInline = false;
        });

        await ReplyAsync("Here's the legality report!", false, builder.Build()).ConfigureAwait(false);
    }
}