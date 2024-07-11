using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class BotModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("BotStatus")]
    [Summary("Gets the status of the bots.")]
    [RequireSudo]
    public async Task GetStatusAsync()
    {
        var me = SysCord<T>.Runner;
        var bots = me.Bots.Select(z => z.Bot).OfType<PokeRoutineExecutorBase>().ToArray();
        if (bots.Length == 0)
        {
            await ReplyAsync("No bots configured.").ConfigureAwait(false);
            return;
        }

        var summaries = bots.Select(GetDetailedSummary);
        var lines = string.Join(Environment.NewLine, summaries);
        await ReplyAsync(Format.Code(lines)).ConfigureAwait(false);
    }

    [Command("BotStart")]
    [Summary("Starts bot at the first IP address/port.")]
    [RequireSudo]
    public async Task StartBotAsync()
    {
        await StartBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
    }

    [Command("BotStart")]
    [Summary("Starts a bot by IP address/port.")]
    [RequireSudo]
    public async Task StartBotAsync([Summary("IP Address")] string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Start();
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Start.").ConfigureAwait(false);
    }

    [Command("BotStartall")]
    [Summary("Starts All Bots.")]
    [RequireSudo]
    public async Task StartBotAllAsync([Summary("IP Address")] string ip)
    {
        await BotStartAll().ConfigureAwait(false);
    }

    [Command("BotStop")]
    [Summary("Stops the bot at the first IP address/port.")]
    [RequireSudo]
    public async Task StopBotAsync()
    {
        await StopBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
    }

    [Command("BotStop")]
    [Summary("Stops a bot by IP address/port.")]
    [RequireSudo]
    public async Task StopBotAsync([Summary("IP Address")] string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Stop();
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Stop.").ConfigureAwait(false);
    }

    [Command("BotStopAll")]
    [Summary("Stops the bot at the first IP address/port.")]
    [RequireSudo]
    public async Task StopAllBotAsync()
    {
        await BotStopAll().ConfigureAwait(false);
    }

    [Command("BotIdle")]
    [Alias("botPause")]
    [Summary("Commands a bot to Idle at first IP address/port.")]
    [RequireSudo]
    public async Task IdleBotAsync()
    {
        await IdleBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
    }

    [Command("BotIdle")]
    [Alias("botPause")]
    [Summary("Commands a bot to Idle by IP address/port.")]
    [RequireSudo]
    public async Task IdleBotAsync([Summary("IP Address")] string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Pause();
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Idle.").ConfigureAwait(false);
    }

    [Command("BotChange")]
    [Summary("Changes the routine of a bot at first IP.")]
    [RequireSudo]
    public async Task ChangeTaskAsync([Summary("Routine enum name")] PokeRoutineType task)
    {
        await ChangeTaskAsync(IPBotHelper<T>.Get(SysCord<T>.Runner), task);
    }

    [Command("BotChange")]
    [Summary("Changes the routine of a bot (trades).")]
    [RequireSudo]
    public async Task ChangeTaskAsync([Summary("IP Address")] string ip, [Summary("Routine enum name")] PokeRoutineType task)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Bot.Config.Initialize(task);
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to do {task} as its next task.").ConfigureAwait(false);
    }

    [Command("BotRestart")]
    [Summary("Restarts the bot at the first IP address")]
    [RequireSudo]
    public async Task RestartBotAsync()
    {
        await RestartBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
    }

    [Command("BotRestart")]
    [Summary("Restarts the bot(s) by IP address(es).")]
    [RequireSudo]
    public async Task RestartBotAsync([Summary("Comma Separated IP Addresses")] string ipAddressesCommaSeparated)
    {
        var ips = ipAddressesCommaSeparated.Split(',');
        foreach (var ip in ips)
        {
            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            c.Reset();
            bot.Start();
            await Context.Channel.EchoAndReply($"The bot at {ip} ({c.Label}) has been commanded to Restart.").ConfigureAwait(false);
        }
    }

    [Command("BotRestartAll")]
    [Summary("Restarts the bot at the first IP address")]
    [RequireSudo]
    public async Task RestartAllBotAsync()
    {
        await BotRestartAll().ConfigureAwait(false);
    }

    [Command("Peek")]
    [Summary("Take and send a screenshot from the first available bot.")]
    [RequireSudo]
    public async Task Peek()
    {
        var source = new CancellationTokenSource();
        var token = source.Token;

        var bot = SysCord<T>.Runner.GetBot(IPBotHelper<T>.Get(SysCord<T>.Runner));
        if (bot == null)
        {
            await ReplyAsync($"No bots available to take a screenshot.").ConfigureAwait(false);
            return;
        }

        var c = bot.Bot.Connection;
        var bytes = await c.PixelPeek(token).ConfigureAwait(false);
        if (bytes.Length == 1)
        {
            await ReplyAsync($"Failed to take a screenshot for bot at {bot.Bot.Config.Connection.IP}. Is the bot connected?").ConfigureAwait(false);
            return;
        }
        MemoryStream ms = new(bytes);

        var img = "cap.jpg";
        var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = Color.Purple }.WithFooter(new EmbedFooterBuilder { Text = $"Captured image from bot at address {bot.Bot.Config.Connection.IP}." });
        await Context.Channel.SendFileAsync(ms, img, "", false, embed: embed.Build());
    }

    [Command("PeekAll")]
    [Summary("Take and send a screenshot from all connected bots.")]
    [RequireSudo]
    public async Task AllPeek()
    {
        var source = new CancellationTokenSource();
        var token = source.Token;

        var bots = SysCord<T>.Runner.Bots;
        if (bots.Count == 0)
        {
            await ReplyAsync($"No bots available to take a screenshot.").ConfigureAwait(false);
            return;
        }

        foreach (var bot in bots)
        {
            var c = bot.Bot.Connection;
            var bytes = await c.PixelPeek(token).ConfigureAwait(false);

            if (bytes.Length == 1)
            {
                await ReplyAsync($"Failed to take a screenshot for bot at {bot.Bot.Config.Connection.IP}. Is the bot connected?").ConfigureAwait(false);
                continue;
            }

            MemoryStream ms = new(bytes);
            var img = "cap.jpg";

            var embed = new EmbedBuilder
            {
                ImageUrl = $"attachment://{img}",
                Color = Color.Purple
            }
            .WithFooter(new EmbedFooterBuilder
            {
                Text = $"Captured image from bot at address {bot.Bot.Config.Connection.IP}."
            });

            await Context.Channel.SendFileAsync(ms, img, "", false, embed: embed.Build());
        }
    }

    [Command("Peek")]
    [Summary("Take and send a screenshot from the Switch with the specified IP Address.")]
    [RequireSudo]
    public async Task Peek([Summary("IP Address")] string address)
    {
        var source = new CancellationTokenSource();
        var token = source.Token;

        var bot = SysCord<T>.Runner.GetBot(address);
        if (bot == null)
        {
            await ReplyAsync($"No bot found with the specified address ({address}).").ConfigureAwait(false);
            return;
        }

        var c = bot.Bot.Connection;
        var bytes = await c.PixelPeek(token).ConfigureAwait(false);
        if (bytes.Length == 1)
        {
            await ReplyAsync($"Failed to take a screenshot for bot at {address}. Is the bot connected?").ConfigureAwait(false);
            return;
        }
        MemoryStream ms = new(bytes);

        var img = "cap.jpg";
        var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = Color.Purple }.WithFooter(new EmbedFooterBuilder { Text = $"Captured image from bot at address {address}." });
        await Context.Channel.SendFileAsync(ms, img, "", false, embed: embed.Build());
    }

    private async Task BotRestartAll()
    {
        var bots = GetAllBots();
        if (bots.Count == 0)
        {
            await ReplyAsync("No bots are currently running.").ConfigureAwait(false);
            return;
        }

        var crlf = bots.Any(b => b.Bot is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true });
        foreach (var bot in bots)
        {
            var c = bot.Bot.Connection;
            c.Reset();
            bot.Start();
        }
        await Context.Channel.EchoAndReply($"All Bots have been commanded to Restart.").ConfigureAwait(false);
    }

    private async Task BotStartAll()
    {
        var bots = GetAllBots();
        if (bots.Count == 0)
        {
            await ReplyAsync("No bots are currently running.").ConfigureAwait(false);
            return;
        }

        var crlf = bots.Any(b => b.Bot is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true });
        foreach (var bot in bots)
        {
            bot.Start();
        }
        await Context.Channel.EchoAndReply($"All Bots have been commanded to Start.").ConfigureAwait(false);
    }

    private async Task BotStopAll()
    {
        var bots = GetAllBots();
        if (bots.Count == 0)
        {
            await ReplyAsync("No bots are currently running.").ConfigureAwait(false);
            return;
        }

        var crlf = bots.Any(b => b.Bot is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true });
        foreach (var bot in bots)
        {
            bot.Stop();
        }
        await Context.Channel.EchoAndReply($"All Bots have been commanded to Stop.").ConfigureAwait(false);
    }

    private static List<BotSource<PokeBotState>> GetAllBots()
    {
        var r = SysCord<T>.Runner;
        return r.Bots.FindAll(x => x.IsRunning);
    }

    private static string GetDetailedSummary(PokeRoutineExecutorBase z)
    {
        return $"- {z.Connection.Name} | {z.Connection.Label} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss} | {z.LastLogged}";
    }
}