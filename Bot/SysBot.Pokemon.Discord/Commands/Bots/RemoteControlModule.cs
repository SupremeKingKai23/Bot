using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Remotely controls a bot.")]
public class RemoteControlModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("Click")]
    [Summary("Clicks the specified button.")]
    [RequireRoleAccess(nameof(DiscordManager.RolesRemoteControl))]
    public async Task ClickAsync([Summary("Switch Button")] SwitchButton b)
    {
        var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await ReplyAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(b, bot).ConfigureAwait(false);
    }

    [Command("Click")]
    [Summary("Clicks the specified button.")]
    [RequireSudo]
    public async Task ClickAsync([Summary("IP Address")] string ip, [Summary("Switch Button")] SwitchButton b)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(b, bot).ConfigureAwait(false);
    }

    [Command("SetStick")]
    [Summary("Sets the stick to the specified position.")]
    [RequireRoleAccess(nameof(DiscordManager.RolesRemoteControl))]
    public async Task SetStickAsync([Summary("Switch Joystick")] SwitchStick s, short x, short y, ushort ms = 1_000)
    {
        var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await ReplyAsync($"No bot is available to execute your command: {s}").ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
    }

    [Command("SetStick")]
    [Summary("Sets the stick to the specified position.")]
    [RequireSudo]
    public async Task SetStickAsync([Summary("IP Address")] string ip, [Summary("Switch Joystick")] SwitchStick s, short x, short y, ushort ms = 1_000)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
    }

    [Command("SetScreenOn")]
    [Alias("screenOn", "scrOn")]
    [Summary("Turns the screen on")]
    [RequireSudo]
    public async Task SetScreenOnAsync()
    {
        await SetScreen(true, IPBotHelper<T>.Get(SysCord<T>.Runner)).ConfigureAwait(false);
    }

    [Command("SetScreenOn")]
    [Alias("screenOn", "scrOn")]
    [Summary("Turns the screen on")]
    [RequireSudo]
    public async Task SetScreenOnAsync([Summary("IP Address")][Remainder] string ip)
    {
        await SetScreen(true, ip).ConfigureAwait(false);
    }

    [Command("SetAllScreensOn")]
    [Alias("AllscreensOn", "AllscrOn")]
    [Summary("Turns the screen \"On\" for all bots")]
    [RequireSudo]
    public async Task SetScreenOnAllAsync()
    {
        await SetAllScreens(true).ConfigureAwait(false);
    }

    [Command("SetScreenOff")]
    [Alias("screenOff", "scrOff")]
    [Summary("Turns the screen off")]
    [RequireSudo]
    public async Task SetScreenOffAsync()
    {
        await SetScreen(false, IPBotHelper<T>.Get(SysCord<T>.Runner)).ConfigureAwait(false);
    }

    [Command("SetScreenOff")]
    [Alias("screenOff", "scrOff")]
    [Summary("Turns the screen off")]
    [RequireSudo]
    public async Task SetScreenOffAsync([Summary("IP Address")][Remainder] string ip)
    {
        await SetScreen(false, ip).ConfigureAwait(false);
    }

    [Command("SetAllScreensOff")]
    [Alias("AllscreensOff", "allscrOff")]
    [Summary("Turns the screen \"Off\" for all bots")]
    [RequireSudo]
    public async Task SetScreenOffAllAsync()
    {
        await SetAllScreens(false).ConfigureAwait(false);
    }

    private async Task SetScreen(bool on, string ip)
    {
        var bot = GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.SetScreen(on ? ScreenState.On : ScreenState.Off, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync($"Screen state set to: {(on ? "**On**" : "**Off**")}").ConfigureAwait(false);
    }

    private async Task SetAllScreens(bool on)
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
            await bot.Bot.Connection.SendAsync(SwitchCommand.SetScreen(on ? ScreenState.On : ScreenState.Off, crlf), CancellationToken.None).ConfigureAwait(false);
        }

        await ReplyAsync($"Screen state for all bots set to: {(on ? "**On**" : "**Off**")}").ConfigureAwait(false);
    }

    private static BotSource<PokeBotState>? GetBot(string ip)
    {
        var r = SysCord<T>.Runner;
        return r.GetBot(ip) ?? r.Bots.Find(x => x.IsRunning); // safe fallback for users who mistype IP address for single bot instances
    }

    private static List<BotSource<PokeBotState>> GetAllBots()
    {
        var r = SysCord<T>.Runner;
        return r.Bots.FindAll(x => x.IsRunning);
    }

    private async Task ClickAsyncImpl(SwitchButton button, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(typeof(SwitchButton), button))
        {
            await ReplyAsync($"Unknown button value: {button}").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.Click(button, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync($"{b.Connection.Name} has performed: {button}").ConfigureAwait(false);
    }

    private async Task SetStickAsyncImpl(SwitchStick s, short x, short y, ushort ms, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(typeof(SwitchStick), s))
        {
            await ReplyAsync($"Unknown stick: {s}").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.SetStick(s, x, y, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync($"{b.Connection.Name} has performed: {s}").ConfigureAwait(false);
        await Task.Delay(ms).ConfigureAwait(false);
        await b.Connection.SendAsync(SwitchCommand.ResetStick(s, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync($"{b.Connection.Name} has reset the stick position.").ConfigureAwait(false);
    }

    private bool IsRemoteControlBot(RoutineExecutor<PokeBotState> botstate)
        => botstate is RemoteControlBotLGPE or RemoteControlBotSWSH or RemoteControlBotBS or RemoteControlBotLA or RemoteControlBotSV;
}