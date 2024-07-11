using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SysBot.Pokemon.SpecialRequests;

namespace SysBot.Pokemon.Discord;

[Summary("Special Requests Commands.")]
public class SpecialRequestModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private readonly ExtraCommandUtil<T> Util = new();

    [Command("SpecialRequest")]
    [Alias("sr")]
    [Summary("Adds the user to the Special Request Queue")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task SpecialRequestAsync([Summary("Trade Code")] int code)
    {
        var lgcode = Info.GetRandomLGTradeCode();
        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, new T(), PokeRoutineType.SpecialRequest, PokeTradeType.SpecialRequest, Context.User, lgcode, true).ConfigureAwait(false);
    }

    [Command("SpecialRequest")]
    [Alias("sr")]
    [Summary("Adds the user to the Special Request Queue.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task SpecialRequestAsync()
    {
        var code = Info.GetRandomTradeCode();
        await SpecialRequestAsync(code).ConfigureAwait(false);
    }

    [Command("SpecialRequest")]
    [Alias("sr")]
    [Summary("Emulates Event Distribution of Requested Event Tag")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task SpecialRequestAsync([Summary("Wondercard Tag")] string wcname)
    {
        var code = Info.GetRandomTradeCode();
        await SpecialRequestAsync(code, wcname).ConfigureAwait(false);
    }

    [Command("SpecialRequest")]
    [Alias("sr")]
    [Summary("Emulates Event Distribution of Requested Event Tag.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task SpecialRequestAsync([Summary("Trade Code")] int code, [Summary("Wondercard Tag")] string wcname)
    {
        var lgcode = Info.GetRandomLGTradeCode();
        var sig = Context.User.GetFavor();
        var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
        var sav = SaveUtil.GetBlankSAV(trainer.Version, trainer.OT);
        var pk = LoadEvent<T>(wcname.Replace("pls", "").ToLower(), sav, Info.Hub.Config.Folder.SpecialRequestWCFolder);

        if (pk is not null)
        {
            await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.SpecialRequest, PokeTradeType.Specific, Context.User, lgcode, true).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync("This isn't a valid request!");
        }
    }

    [Command("SpecialRequestuser")]
    [Alias("sru")]
    [Summary("Emulates Event Distribution of Requested Event Tag for the Mentioned User.")]
    [RequireSudo]
    public async Task SpecialRequestAsync([Summary("Mentioned User")] SocketUser usr, [Summary("Wondercard Tag")] string wcname)
    {
        var code = Info.GetRandomTradeCode();
        await SpecialRequestAsync(usr, code, wcname).ConfigureAwait(false);
    }

    [Command("SpecialRequestuser")]
    [Alias("sru")]
    [Summary("Emulates Event Distribution of Requested Event Tag for the Mentioned User.")]
    [RequireSudo]
    public async Task SpecialRequestAsync([Summary("Mentioned User")] SocketUser usr, [Summary("Trade Code")] int code, [Summary("Wondercard Tag")] string wcname)
    {
        var lgcode = Info.GetRandomLGTradeCode();
        var sig = Context.User.GetFavor();
        var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
        var sav = SaveUtil.GetBlankSAV(trainer.Version, trainer.OT);
        var pk = LoadEvent<T>(wcname.Replace("pls", "").ToLower(), sav, Info.Hub.Config.Folder.SpecialRequestWCFolder);

        if (pk is not null)
        {
            await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.SpecialRequest, PokeTradeType.Specific, usr, lgcode, true, true, true).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync("This isn't a valid request!");
        }
    }

    [Command("SpecialRequestPool")]
    [Alias("srp", "srpool")]
    [Summary("Show a list of Pokémon available for SpecialRequest Distributions.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task DisplaySpecialRequestPoolCountAsync()
    {
        string folderPath = Info.Hub.Config.Folder.SpecialRequestWCFolder;

        if (folderPath == null)
        {
            await ReplyAsync("No Folder Currently Set.");
        }
        else
        {
            var fileNames = Directory.GetFiles(folderPath).Select(Path.GetFileNameWithoutExtension);
            if (fileNames.Any())
            {
                var lines = fileNames.Select((z, i) => $"{i + 1}: {z}");
                var msg = string.Join("\n", lines);
                await Util.ListUtil(Context, "Available SpecialRequests Distributions", msg).ConfigureAwait(false);
            }
            else await ReplyAsync("No files found.").ConfigureAwait(false);
        }
    }
}