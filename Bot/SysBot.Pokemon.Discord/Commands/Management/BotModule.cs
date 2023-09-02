using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class BotModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        [Command("botStatus")]
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

        private static string GetDetailedSummary(PokeRoutineExecutorBase z)
        {
            return $"- {z.Connection.Name} | {z.Connection.Label} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss} | {z.LastLogged}";
        }

        [Command("botStart")]
        [Summary("Starts bot at the first IP address/port.")]
        [RequireSudo]
        public async Task StartBotAsync()
        {
            await StartBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
        }

        [Command("botStart")]
        [Summary("Starts a bot by IP address/port.")]
        [RequireSudo]
        public async Task StartBotAsync(string ip)
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

        [Command("botStartall")]
        [Summary("Starts All Bots.")]
        [RequireSudo]
        public async Task StartBotAllAsync(string ip)
        {
            await BotStartAll().ConfigureAwait(false);
        }

        [Command("botStop")]
        [Summary("Stops the bot at the first IP address/port.")]
        [RequireSudo]
        public async Task StopBotAsync()
        {
            await StopBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
        }

        [Command("botStop")]
        [Summary("Stops a bot by IP address/port.")]
        [RequireSudo]
        public async Task StopBotAsync(string ip)
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

        [Command("botStopAll")]
        [Summary("Stops the bot at the first IP address/port.")]
        [RequireSudo]
        public async Task StopAllBotAsync()
        {
            await BotStopAll().ConfigureAwait(false);
        }

        [Command("botIdle")]
        [Alias("botPause")]
        [Summary("Commands a bot to Idle at first IP address/port.")]
        [RequireSudo]
        public async Task IdleBotAsync()
        {
            await IdleBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
        }

        [Command("botIdle")]
        [Alias("botPause")]
        [Summary("Commands a bot to Idle by IP address/port.")]
        [RequireSudo]
        public async Task IdleBotAsync(string ip)
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

        [Command("botChange")]
        [Summary("Changes the routine of a bot at first IP.")]
        [RequireSudo]
        public async Task ChangeTaskAsync([Summary("Routine enum name")] PokeRoutineType task)
        {
            await ChangeTaskAsync(IPBotHelper<T>.Get(SysCord<T>.Runner), task);
        }

        [Command("botChange")]
        [Summary("Changes the routine of a bot (trades).")]
        [RequireSudo]
        public async Task ChangeTaskAsync(string ip, [Summary("Routine enum name")] PokeRoutineType task)
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

        [Command("botRestart")]
        [Summary("Restarts the bot at the first IP address")]
        [RequireSudo]
        public async Task RestartBotAsync()
        {
            await RestartBotAsync(IPBotHelper<T>.Get(SysCord<T>.Runner));
        }

        [Command("botRestart")]
        [Summary("Restarts the bot(s) by IP address(es), separated by commas.")]
        [RequireSudo]
        public async Task RestartBotAsync(string ipAddressesCommaSeparated)
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

        [Command("botRestartAll")]
        [Summary("Restarts the bot at the first IP address")]
        [RequireSudo]
        public async Task RestartAllBotAsync()
        {
            await BotRestartAll().ConfigureAwait(false);
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
    }
}
