using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Link Code trades")]
    public class TradeModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

        [Command("tradeList")]
        [Alias("tl")]
        [Summary("Prints the users in the trade queues.")]
        [RequireSudo]
        public async Task GetTradeListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the provided Pokémon file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach([Summary("Trade Code")] int code)
        {
            var sig = Context.User.GetFavor();
            var lgcode = Info.GetRandomLGTradeCode();
            await TradeAsyncAttach(code, sig, Context.User, lgcode, true).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
        {
            var lgcode = Info.GetRandomLGTradeCode();
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            try
            { 
                var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
                var sav = SaveUtil.GetBlankSAV((GameVersion)trainer.Game, trainer.OT);
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                if (!la.Valid)
                    pkm.ClearMemories();
                var la2 = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                if (pkm is not PB7)
                {
                    pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                }
                if (pkm is not T pk || !la2.Valid)
                {
                    var reason = result == "Timeout" ? $"That {spec} set took too long to generate." : result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." : $"I wasn't able to create a {spec} from that set.";
                    var imsg = $"Oops! {reason}";
                    if (result == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                    await ReplyAsync(imsg).ConfigureAwait(false);
                    return;
                }
                pk.ResetPartyStats();

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, lgcode).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (set != null)
                {
                    LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                    var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                    await ReplyAsync(msg).ConfigureAwait(false);
                }
                else
                {
                    LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                    var msg = $"Oops! An unexpected problem happened with thas Showdown Set.";
                    await ReplyAsync(msg).ConfigureAwait(false);
                }
            }
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsync(code, content).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the attached file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach()
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttach(code).ConfigureAwait(false);
        }

        [Command("hiddentrade")]
        [Alias("ht")]
        [Summary("Makes the bot trade you the provided Pokémon file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttachNoEmbed([Summary("Trade Code")] int code)
        {
            var sig = Context.User.GetFavor();
            var lgcode = Info.GetRandomLGTradeCode();
            await TradeAsyncAttach(code, sig, Context.User, lgcode, false).ConfigureAwait(false);
        }

        [Command("hiddentrade")]
        [Alias("ht")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncNoEmbed([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
        {
            var lgcode = Info.GetRandomLGTradeCode();
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                bool pla = typeof(T) == typeof(PA8);

                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                bool memes = Info.Hub.Config.Trade.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
                if (memes)
                    return;

                if (pkm is not T pk || !la.Valid)
                {
                    var reason = result == "Timeout" ? $"That {spec} set took too long to generate." : result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." : $"I wasn't able to create a {spec} from that set.";
                    var imsg = $"Oops! {reason}";
                    if (result == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                    await ReplyAsync(imsg).ConfigureAwait(false);
                    return;
                }
                pk.ResetPartyStats();

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, lgcode,  false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                await ReplyAsync(msg).ConfigureAwait(false);
            }
        }

        [Command("hiddentrade")]
        [Alias("ht")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncNoEmbed([Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncNoEmbed(code, content).ConfigureAwait(false);
        }

        [Command("hiddentrade")]
        [Alias("ht")]
        [Summary("Makes the bot trade you the attached file without displaying an embed is they are enabled.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttachNoEmbed()
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttachNoEmbed(code).ConfigureAwait(false);
        }

        [Command("banTrade")]
        [Alias("bt")]
        [RequireSudo]
        public async Task BanTradeAsync([Summary("Online ID")] ulong nnid, string comment)
        {
            SysCordSettings.HubConfig.TradeAbuse.BannedIDs.AddIfNew(new[] { GetReference(nnid, comment) });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        private RemoteControlAccess GetReference(ulong id, string comment) => new()
        {
            ID = id,
            Name = id.ToString(),
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
        };

        [Command("tradeuser")]
        [Alias("tu")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireSudo]
        public async Task TradeUserAsync([Summary("Mentioned User")] SocketUser usr, [Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await TradeUserAsync(usr, code, content).ConfigureAwait(false);
        }

        [Command("tradeuser")]
        [Alias("tu")]
        [Summary("Makes the bot trade the mentioned user the provided Showdown Set.")]
        [RequireSudo]
        public async Task TradeUserAsync([Summary("Mentioned User")] SocketUser usr, [Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
        {
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = usr.GetFavor();
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);

                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                bool memes = Info.Hub.Config.Trade.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
                if (memes)
                    return;

                if (pkm is not T pk || !la.Valid)
                {
                    var reason = result == "Timeout" ? $"That {spec} set took too long to generate." : result == "VersionMismatch" ? "Request refused: PKHeX and Auto-Legality Mod version mismatch." : $"I wasn't able to create a {spec} from that set.";
                    var imsg = $"Oops! {reason}";
                    if (result == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                    await ReplyAsync(imsg).ConfigureAwait(false);
                    return;
                }
                pk.ResetPartyStats();

                await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr, lgcode, true, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                await ReplyAsync(msg).ConfigureAwait(false);
            }
        }

        [Command("tradeUser")]
        [Alias("tu", "tradeOther")]
        [Summary("Makes the bot trade the mentioned user the attached file.")]
        [RequireSudo]
        public async Task TradeAsyncAttachUser([Summary("Mentioned User")] SocketUser usr, [Summary("Trade Code")] int code)
        {
            if (Context.Message.MentionedUsers.Count > 1)
            {
                await ReplyAsync("Too many mentions. Queue one user at a time.").ConfigureAwait(false);
                return;
            }

            if (Context.Message.MentionedUsers.Count == 0)
            {
                await ReplyAsync("A user must be mentioned in order to do this.").ConfigureAwait(false);
                return;
            }

            var sig = usr.GetFavor();

            var lgcode = Info.GetRandomLGTradeCode();
            await TradeAsyncAttach(code, sig, usr, lgcode, true).ConfigureAwait(false);
        }

        [Command("tradeUser")]
        [Alias("tu", "tradeOther")]
        [Summary("Makes the bot trade the mentioned user the attached file.")]
        [RequireSudo]
        public async Task TradeAsyncAttachUser([Summary("Mentioned User")] SocketUser usr)
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttachUser(usr, code).ConfigureAwait(false);
        }

        private async Task TradeAsyncAttach(int code, RequestSignificance sig, SocketUser usr, List<PictoCodes> lgcode, bool displayEmbeds)
        {
            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment == default)
            {
                await ReplyAsync("No attachment provided!").ConfigureAwait(false);
                return;
            }

            var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
            var pk = GetRequest(att);
            if (pk == null)
            {
                await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
                return;
            }

            await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr, lgcode, displayEmbeds).ConfigureAwait(false);
        }

        private static T? GetRequest(Download<PKM> dl)
        {
            if (!dl.Success)
                return null;
            return dl.Data switch
            {
                null => null,
                T pk => pk,
                _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
            };
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, T pk, RequestSignificance sig, SocketUser usr, List<PictoCodes> lgcode, bool displayEmbeds = true, bool userTrade = false)
        {
            var (canBeTraded, errorMessage) = pk.CanBeTraded();
            if (!canBeTraded)
            {
                await ReplyAsync($"{Context.User.Mention}, {errorMessage}").ConfigureAwait(false);
                return;
            }
            
            var la = new LegalityAnalysis(pk);
            if (!la.Valid)
               pk.CurrentHandler = 1;
            var la2= new LegalityAnalysis(pk);
            if (!la2.Valid)
            {
                await ReplyAsync($"{Context.User.Mention}, {typeof(T).Name} attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                return;
            }
            
            await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr, lgcode, displayEmbeds, true, userTrade).ConfigureAwait(false);
        }
    }
}
