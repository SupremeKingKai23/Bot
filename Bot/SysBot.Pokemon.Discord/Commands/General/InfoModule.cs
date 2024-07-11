using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

// src: https://github.com/foxbot/patek/blob/master/src/Patek/Modules/InfoModule.cs
// ISC License (ISC)
// Copyright 2017, Christopher F. <foxbot@protonmail.com>
public class InfoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;
    private const string Version = "v9.9";
    private const string dev = "https://www.mergebot.net";

    [Command("Info")]
    [Alias("about", "whoami", "owner")]
    public async Task InfoAsync()
    {
        var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
        var game = typeof(T) switch
        {
            Type type when type == typeof(PK9) => "SV",
            Type type when type == typeof(PK8) => "SWSH",
            Type type when type == typeof(PA8) => "PLA",
            Type type when type == typeof(PB8) => "BDSP",
            _ => "LGPE"
        };
        var lang = Hub.Config.CurrentLanguage;

        var builder = new EmbedBuilder
        {
            Title = "Here's a bit about me!",
            Color = Color.Green,
            ImageUrl = "https://i.imgur.com/9ITMeg9.png"
        };

        builder.WithDescription(
            $"- {Format.Bold("Owner")}: {app.Owner.Username}\n" +
            (Context.User.Id == 195756980873199618 ? $"- {Format.Bold($"Owner ID")}: {app.Owner.Id}\n" : "") +
            $"- {Format.Bold("Current Game")}: {game}\n" +
            $"- {Format.Bold("Current Language")}: {lang}\n" +
            $"- {Format.Bold("MergeBot Version")}: {Version}\n" +
            $"- {Format.Bold("PKHeX.Core Version")}: {GetVersionInfo("PKHeX.Core")}\n" +
            $"- {Format.Bold("AutoLegality Version")}: {GetVersionInfo("PKHeX.Core.AutoMod")}\n" +
            $"- {Format.Bold("Built on kwsch [SysBot.NET](https://github.com/kwsch/SysBot.NET)")}\n" +
            $"- {Format.Bold($"Dev Server: [MergeBot Central]({dev})")}"
            );

        await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
    }


    [Command("Status")]
    [Alias("stats")]
    [Summary("Gets the status of the bot environment.")]
    public async Task GetStatusAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;

        var builder = new EmbedBuilder
        {
            Color = Color.Gold,
        };

        var runner = SysCord<T>.Runner;
        var allBots = runner.Bots.ConvertAll(z => z.Bot);
        var botCount = allBots.Count;
        builder.AddField(x =>
        {
            x.Name = "Summary";
            x.Value =
                $"Bot Count: {botCount}\n" +
                $"Bot State: {SummarizeBots(allBots)}\n" +
                $"Pool Count: {hub.Ledy.Pool.Count}\n";
            x.IsInline = false;
        });

        if (Hub.Config.Counts.EmitCountsOnStatusCheck)
        {
            builder.AddField(x =>
            {
                var bots = allBots.OfType<ICountBot>();
                var lines = bots.SelectMany(z => z.Counts.GetNonZeroCounts()).Distinct();
                var msg = string.Join("\n", lines);
                if (string.IsNullOrWhiteSpace(msg))
                    msg = "Nothing counted yet!";
                x.Name = "Counts";
                x.Value = msg;
                x.IsInline = false;
            });
        }

        var queues = hub.Queues.AllQueues;
        int count = 0;
        foreach (var q in queues)
        {
            var c = q.Count;
            if (c == 0)
                continue;

            var nextMsg = GetNextName(q);
            builder.AddField(x =>
            {
                x.Name = $"{q.Type} Queue";
                x.Value =
                    $"Next: {nextMsg}\n" +
                    $"Count: {c}\n";
                x.IsInline = false;
            });
            count += c;
        }

        if (count == 0)
        {
            builder.AddField(x =>
            {
                x.Name = "Queues are empty.";
                x.Value = "Nobody in line!";
                x.IsInline = false;
            });
        }

        await ReplyAsync("Bot Status", false, builder.Build()).ConfigureAwait(false);
    }

    private static string GetNextName(PokeTradeQueue<T> q)
    {
        var next = q.TryPeek(out var detail, out _);
        if (!next)
            return "None!";

        var name = detail.Trainer.TrainerName;

        // show detail of trade if possible
        var nick = detail.TradeData.Nickname;
        if (!string.IsNullOrEmpty(nick))
            name += $" - {nick}";
        return name;
    }

    private static string SummarizeBots(IReadOnlyCollection<RoutineExecutor<PokeBotState>> bots)
    {
        if (bots.Count == 0)
            return "No bots configured.";
        var summaries = bots.Select(z => $"- {z.GetSummary()}");
        return Environment.NewLine + string.Join(Environment.NewLine, summaries);
    }

    private static string GetVersionInfo(string assemblyName)
    {
        const string _default = "Unknown";
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == assemblyName);
        if (assembly is null)
            return _default;

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute is null)
            return _default;

        var info = attribute.InformationalVersion;
        var split = info.Split('+');
        if (split.Length >= 2)
        {
            var versionParts = split[0].Split('.');
            if (versionParts.Length == 3)
            {
                var major = versionParts[0].PadLeft(2, '0');
                var minor = versionParts[1].PadLeft(2, '0');
                var patch = versionParts[2].PadLeft(2, '0');
                return $"{major}.{minor}.{patch}";
            }
        }
        return _default;
    }
}