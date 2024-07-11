using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static PKHeX.Core.AutoMod.Aesthetics;

namespace SysBot.Pokemon.Discord;

public class TradeStartModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private class TradeStartAction(ulong id, Action<PokeRoutineExecutorBase, PokeTradeDetail<T>> messager, string channel) : ChannelAction<PokeRoutineExecutorBase, PokeTradeDetail<T>>(id, messager, channel);

    private static readonly Dictionary<ulong, TradeStartAction> Channels = [];

    private static readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

    private static void Remove(TradeStartAction entry)
    {
        Channels.Remove(entry.ChannelID);
        SysCord<T>.Runner.Hub.Queues.Forwarders.Remove(entry.Action);
    }

    private static DiscordSocketClient _client = new();

    public static void RestoreTradeStarting(DiscordSocketClient discord)
    {
        var cfg = SysCordSettings.Settings;
        foreach (var ch in cfg.TradeStartingChannels)
        {
            if (discord.GetChannel(ch.ID) is ISocketMessageChannel c)
                AddLogChannel(c, ch.ID);
        }
        _client = discord;
        LogUtil.LogInfo("Added Trade Start Notification to Discord channel(s) on Bot startup.", "Discord");
    }

    public static bool IsStartChannel(ulong cid)
    {
        return Channels.TryGetValue(cid, out _);
    }

    [Command("StartHere")]
    [Summary("Makes the bot log trade starts to the channel.")]
    [RequireSudo]
    public async Task AddLogAsync()
    {
        var c = Context.Channel;
        var cid = c.Id;
        if (Channels.TryGetValue(cid, out _))
        {
            await ReplyAsync("Already logging here.").ConfigureAwait(false);
            return;
        }

        AddLogChannel(c, cid);

        // Add to discord global loggers (saves on program close)
        SysCordSettings.Settings.TradeStartingChannels.AddIfNew(new[] { GetReference(Context.Channel) });
        await ReplyAsync("Added Start Notification output to this channel!").ConfigureAwait(false);
    }

    private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
    {
        void Logger(PokeRoutineExecutorBase bot, PokeTradeDetail<T> detail)
        {
            if (detail.Type == PokeTradeType.Random)
                return;
            if (Hub.Config.Trade.EmbedSettings.UseTradeStartEmbeds)
                c.SendMessageAsync(embed: GetEmbed(bot, detail));
            else
                c.SendMessageAsync(GetMessage(bot, detail));
        }

        Action<PokeRoutineExecutorBase, PokeTradeDetail<T>> l = Logger;
        SysCord<T>.Runner.Hub.Queues.Forwarders.Add(l);

        static Embed GetEmbed(PokeRoutineExecutorBase bot, PokeTradeDetail<T> detail)
        {
            Embed embed;
            if (detail.Type == PokeTradeType.Specific || detail.Type == PokeTradeType.Giveaway || detail.Type == PokeTradeType.SupportTrade)
            {
                embed = new EmbedBuilder()
                .WithAuthor($"Trade Started", iconUrl: _client.GetUser(detail.Trainer.ID).GetAvatarUrl())
                .WithDescription($"## Sending {detail.Trainer.TrainerName}'s {(detail.MysteryEgg ? "Mystery Egg" : $"{(Species)detail.TradeData.Species}{EmbedHelper<T>.GetFormString(detail.TradeData)}")}")
                .WithColor(EmbedHelper<T>.GetDiscordColor(detail.TradeData.IsShiny ? EmbedHelper<T>.ShinyMap[((Species)detail.TradeData.Species, detail.TradeData.Form)] : (PersonalColor)detail.TradeData.PersonalInfo.Color))
                .WithFooter($"Trade #{detail.ID}", EmbedHelper<T>.GetBallURL(detail.TradeData))
                .WithThumbnailUrl(detail.MysteryEgg ? $"https://raw.githubusercontent.com/BakaKaito/HomeImages/Home3.0/Sprites/128x128/MysteryEgg.png" : TradeExtensions<PK9>.PokeImg(detail.TradeData))
                .WithTimestamp(DateTime.Now)
                .Build();
            }
            else
            {
                var desc = detail.Type switch
                {
                    PokeTradeType.Clone => "Initializing Cloning Pod",
                    PokeTradeType.Dump => "Initializing Pokémon Scanner",
                    PokeTradeType.FixOT => "Initializing AD Detection ",
                    PokeTradeType.SpecialRequest => "Initializing Special Request",
                    _ => "Initializing Trade"
                };
                var imageURL = detail.Type switch
                {
                    PokeTradeType.Clone => "https://i.imgur.com/7L5CfPt.png",
                    PokeTradeType.Dump => "https://i.imgur.com/iwCCCAY.gif",
                    PokeTradeType.FixOT => "https://i.imgur.com/n9nuReu.png",
                    PokeTradeType.SpecialRequest => "https://i.imgur.com/n9nuReu.png",
                    _ => "https://i.imgur.com/n9nuReu.png"
                };

                embed = new EmbedBuilder()
               .WithAuthor($"Processing {detail.Trainer.TrainerName}", iconUrl: _client.GetUser(detail.Trainer.ID).GetAvatarUrl())
               .WithDescription($"## {desc}")
               .WithColor(Color.Green)
               .WithFooter($"Trade #{detail.ID}")
               .WithThumbnailUrl(imageURL)
               .WithTimestamp(DateTime.Now)
               .Build();
            }

            return embed;
        }

        static string GetMessage(PokeRoutineExecutorBase bot, PokeTradeDetail<T> detail) => $"> [{DateTime.Now:hh:mm:ss}] - {bot.Connection.Label} is now trading (ID {detail.ID}) {detail.Trainer.TrainerName}";

        var entry = new TradeStartAction(cid, l, c.Name);
        Channels.Add(cid, entry);
    }

    [Command("StartInfo")]
    [Summary("Dumps the Start Notification settings.")]
    [RequireSudo]
    public async Task DumpLogInfoAsync()
    {
        foreach (var c in Channels)
            await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
    }

    [Command("StartClear")]
    [Summary("Clears the Start Notification settings in that specific channel.")]
    [RequireSudo]
    public async Task ClearLogsAsync()
    {
        var cfg = SysCordSettings.Settings;
        if (Channels.TryGetValue(Context.Channel.Id, out var entry))
            Remove(entry);
        cfg.TradeStartingChannels.RemoveAll(z => z.ID == Context.Channel.Id);
        await ReplyAsync($"Start Notifications cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
    }

    [Command("StartClearAll")]
    [Summary("Clears all the Start Notification settings.")]
    [RequireSudo]
    public async Task ClearLogsAllAsync()
    {
        foreach (var l in Channels)
        {
            var entry = l.Value;
            await ReplyAsync($"Logging cleared from {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
            SysCord<T>.Runner.Hub.Queues.Forwarders.Remove(entry.Action);
        }
        Channels.Clear();
        SysCordSettings.Settings.TradeStartingChannels.Clear();
        await ReplyAsync("Start Notifications cleared from all channels!").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}