using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotLanguage = SysBot.Pokemon.Helpers.BotLanguage;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Link Code trades")]
public class TradeModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private static BotLanguage Lang => Info.Hub.Config.CurrentLanguage;

    [Command("TradeList")]
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

    [Command("Trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you the provided Pokémon file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncAttach([Summary("Trade Code")] int code)
    {
        List<PictoCodes>? lgcode;
        int tcode;
        if (typeof(T) == typeof(PB7))
        {
            lgcode = ReusableActions.GetLGPETradeCode(code);
            tcode = Info.GetRandomTradeCode();
        }
        else
        {
            lgcode = Info.GetRandomLGTradeCode();
            tcode = code;
        }
        var sig = Context.User.GetFavor();
        await TradeAsyncAttach(tcode, sig, Context.User, lgcode, true).ConfigureAwait(false);
    }

    [Command("Trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        List<PictoCodes>? lgcode;
        int tcode;
        if (typeof(T) == typeof(PB7))
        {
            lgcode = ReusableActions.GetLGPETradeCode(code);
            tcode = Info.GetRandomTradeCode();
        }
        else
        {
            lgcode = Info.GetRandomLGTradeCode();
            tcode = code;
        }
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (set.InvalidLines.Count != 0)
        {
            var msg = LanguageHelper.ParseSet(Lang) + $"\n{string.Join("\n", set.InvalidLines)}";
            await ReplyAsync(msg).ConfigureAwait(false);
            return;
        }

        try
        {
            var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
            var sav = SaveUtil.GetBlankSAV(trainer.Version, trainer.OT);
            var pkm = sav.GetLegal(template, out var result);

            if (Info.Hub.Config.Legality.SetSuggestedMoves || pkm is PA8)
            {
                var movesClone = pkm.Clone();
                movesClone.SetSuggestedMoves();

                var originalMoves = new HashSet<ushort> { pkm.Move1, pkm.Move2, pkm.Move3, pkm.Move4 };
                var clonedMoves = new List<ushort> { movesClone.Move1, movesClone.Move2, movesClone.Move3, movesClone.Move4 };

                static ushort GetNonDuplicateMove(ushort originalMove, List<ushort> possibleMoves, HashSet<ushort> existingMoves)
                {
                    if (originalMove == 0)
                    {
                        foreach (var move in possibleMoves)
                        {
                            if (!existingMoves.Contains(move))
                            {
                                existingMoves.Add(move);
                                return move;
                            }
                        }
                    }
                    return originalMove;
                }

                pkm.Move1 = GetNonDuplicateMove(pkm.Move1, clonedMoves, originalMoves);
                pkm.Move2 = GetNonDuplicateMove(pkm.Move2, clonedMoves, originalMoves);
                pkm.Move3 = GetNonDuplicateMove(pkm.Move3, clonedMoves, originalMoves);
                pkm.Move4 = GetNonDuplicateMove(pkm.Move4, clonedMoves, originalMoves);
            }

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];

            if (pkm is not PB7)
            {
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
            }

            bool memes = Info.Hub.Config.Trade.MiscSettings.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
            if (memes)
                return;

            if (Info.Hub.Config.Legality.SetSuggestedRelearnMoves && pkm is not PA8 or PB7)
                pkm.SetRelearnMoves(la);

            if (Info.Hub.Config.Legality.SetAllTechnicalRecords)
                if (pkm is ITechRecord tr)
                    tr.SetRecordFlagsAll(la.Info.EvoChainsAllGens.Get(pkm.Context));

            if (pkm is not T pk || !la.Valid)
            {
                var reason = result switch
                {
                    "Timeout" => LanguageHelper.Timeout(Lang, spec),
                    "VersionMismatch" => LanguageHelper.Mismatch(Lang),
                    _ => LanguageHelper.Unable(Lang, spec)
                };
                var oops = LanguageHelper.Oops(Lang);
                var embed = new EmbedBuilder();
                var imsg = $"{Context.User.Mention} - {reason}";
                if (result == "Failed")
                    if (Info.Hub.Config.Legality.EnableHOMETrackerCheck)
                    {
                        if (pkm is PK9 && HomeTransfers.IsHomeTransferOnlySV((Species)pkm.Species, pkm.Form))
                        {
                            var ALMHint = $"{string.Format(BattleTemplateLegality.HOME_TRANSFER_ONLY, spec, "SV")}";
                            embed = LegalityReportEmbed(la, oops, imsg, ALMHint); 
                        }
                        else
                        {
                            var ALMHint = $"{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                            embed = LegalityReportEmbed(la, oops, imsg, ALMHint);
                        }
                    }
                    else
                    {
                        var ALMHint = $"{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                        embed = LegalityReportEmbed(la, oops, imsg, ALMHint);
                    }
                await ReplyAsync(embed : embed.Build()).ConfigureAwait(false);
                return;
            }

            pk.ResetPartyStats();

            var sig = Context.User.GetFavor();
            await AddTradeToQueueAsync(tcode, pk, sig, Context.User, lgcode).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (set != null)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = LanguageHelper.Unexpected(Lang) + $"\n```{string.Join("\n", set.GetSetLines())}```";
                await ReplyAsync(msg).ConfigureAwait(false);
            }
            else
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = LanguageHelper.Unexpected(Lang);
                await ReplyAsync(msg).ConfigureAwait(false);
            }
        }
    }

    [Command("Trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
    {
        if (typeof(T) != typeof(PB7))
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsync(code, content).ConfigureAwait(false);
        }
        else
        {
            var code = Info.GetRandomLGPENumCode();
            await TradeAsync(code == 0 ? 000 : code, content).ConfigureAwait(false);
        }
    }

    [Command("Trade")]
    [Alias("t")]
    [Summary("Makes the bot trade you the attached file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncAttach()
    {
        if (typeof(T) != typeof(PB7))
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttach(code).ConfigureAwait(false);
        }
        else
        {
            var code = Info.GetRandomLGPENumCode();
            await TradeAsyncAttach(code == 0 ? 000 : code).ConfigureAwait(false);
        }
    }

    [Command("TradeUser")]
    [Alias("tu")]
    [Summary("Maakes the bot trade the mentioned user the provided Showdown Set.")]
    [RequireSudo]
    public async Task TradeUserAsync([Summary("Mentioned User")] SocketUser usr, [Summary("Showdown Set")][Remainder] string content)
    {
        if (typeof(T) != typeof(PB7))
        {
            var code = Info.GetRandomTradeCode();
            await TradeUserAsync(usr, code, content).ConfigureAwait(false);
        }
        else
        {
            var code = Info.GetRandomLGPENumCode();
            await TradeUserAsync(usr, code == 0 ? 000 : code, content).ConfigureAwait(false);
        }
    }

    [Command("TradeUser")]
    [Alias("tu")]
    [Summary("Makes the bot trade the mentioned user the provided Showdown Set.")]
    [RequireSudo]
    public async Task TradeUserAsync([Summary("Mentioned User")] SocketUser usr, [Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        List<PictoCodes>? lgcode;
        int tcode;
        if (typeof(T) == typeof(PB7))
        {
            lgcode = ReusableActions.GetLGPETradeCode(code);
            tcode = Info.GetRandomTradeCode();
        }
        else
        {
            lgcode = Info.GetRandomLGTradeCode();
            tcode = code;
        }
        var sig = usr.GetFavor();
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (set.InvalidLines.Count != 0)
        {
            var msg = LanguageHelper.ParseSet(Lang) + $"\n{string.Join("\n", set.InvalidLines)}";
            await ReplyAsync(msg).ConfigureAwait(false);
            return;
        }

        try
        {
            var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
            var sav = SaveUtil.GetBlankSAV(trainer.Version, trainer.OT);
            var pkm = sav.GetLegal(template, out var result);

            if (Info.Hub.Config.Legality.SetSuggestedMoves || pkm is PA8)
            {
                var movesClone = pkm.Clone();
                movesClone.SetSuggestedMoves();

                var originalMoves = new HashSet<ushort> { pkm.Move1, pkm.Move2, pkm.Move3, pkm.Move4 };
                var clonedMoves = new List<ushort> { movesClone.Move1, movesClone.Move2, movesClone.Move3, movesClone.Move4 };

                static ushort GetNonDuplicateMove(ushort originalMove, List<ushort> possibleMoves, HashSet<ushort> existingMoves)
                {
                    if (originalMove == 0)
                    {
                        foreach (var move in possibleMoves)
                        {
                            if (!existingMoves.Contains(move))
                            {
                                existingMoves.Add(move);
                                return move;
                            }
                        }
                    }
                    return originalMove;
                }

                pkm.Move1 = GetNonDuplicateMove(pkm.Move1, clonedMoves, originalMoves);
                pkm.Move2 = GetNonDuplicateMove(pkm.Move2, clonedMoves, originalMoves);
                pkm.Move3 = GetNonDuplicateMove(pkm.Move3, clonedMoves, originalMoves);
                pkm.Move4 = GetNonDuplicateMove(pkm.Move4, clonedMoves, originalMoves);
            }

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];

            if (pkm is not PB7)
            {
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
            }

            bool memes = Info.Hub.Config.Trade.MiscSettings.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
            if (memes)
                return;

            if (Info.Hub.Config.Legality.SetSuggestedRelearnMoves && pkm is not PA8 or PB7)
                pkm.SetRelearnMoves(la);

            if (Info.Hub.Config.Legality.SetAllTechnicalRecords)
                if (pkm is ITechRecord tr)
                    tr.SetRecordFlagsAll(la.Info.EvoChainsAllGens.Get(pkm.Context));

            if (pkm is not T pk || !la.Valid)
            {
                var reason = result switch
                {
                    "Timeout" => LanguageHelper.Timeout(Lang, spec),
                    "VersionMismatch" => LanguageHelper.Mismatch(Lang),
                    _ => LanguageHelper.Unable(Lang, spec)
                };
                var oops = LanguageHelper.Oops(Lang);
                var embed = new EmbedBuilder();
                var imsg = $"{Context.User.Mention} - {reason}";
                if (result == "Failed")
                    if (Info.Hub.Config.Legality.EnableHOMETrackerCheck)
                    {
                        if (pkm is PK9 && HomeTransfers.IsHomeTransferOnlySV((Species)pkm.Species, pkm.Form))
                        {
                            var ALMHint = $"{string.Format(BattleTemplateLegality.HOME_TRANSFER_ONLY, spec, "SV")}";
                            embed = LegalityReportEmbed(la, oops, imsg, ALMHint);
                        }
                        else
                        {
                            var ALMHint = $"{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                            embed = LegalityReportEmbed(la, oops, imsg, ALMHint);
                        }
                    }
                    else
                    {
                        var ALMHint = $"{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                        embed = LegalityReportEmbed(la, oops, imsg, ALMHint);
                    }
                await ReplyAsync(embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            pk.ResetPartyStats();

            await AddTradeToQueueAsync(tcode, pk, sig, usr, lgcode, true, true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
            var msg = LanguageHelper.Unexpected(Lang) + $"\n```{string.Join("\n", set.GetSetLines())}```";
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }

    [Command("TradeUser")]
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

    [Command("TradeUser")]
    [Alias("tu", "tradeOther")]
    [Summary("Makes the bot trade the mentioned user the attached file.")]
    [RequireSudo]
    public async Task TradeAsyncAttachUser([Summary("Mentioned User")] SocketUser usr)
    {
        if (typeof(T) != typeof(PB7))
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttachUser(usr, code).ConfigureAwait(false);
        }
        else
        {
            var code = Info.GetRandomLGPENumCode();
            await TradeAsyncAttachUser(usr, code).ConfigureAwait(false);
        }
    }

    [Command("HiddenTrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you the provided Pokémon file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncAttachNoEmbed([Summary("Trade Code")] int code)
    {
        var sig = Context.User.GetFavor();
        var lgcode = Info.GetRandomLGTradeCode();
        await TradeAsyncAttach(code, sig, Context.User, lgcode, false).ConfigureAwait(false);
    }

    [Command("HiddenTrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncNoEmbed([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        List<PictoCodes>? lgcode;
        int tcode;
        if (typeof(T) == typeof(PB7))
        {
            lgcode = ReusableActions.GetLGPETradeCode(code);
            tcode = Info.GetRandomTradeCode();
        }
        else
        {
            lgcode = Info.GetRandomLGTradeCode();
            tcode = code;
        }
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (set.InvalidLines.Count != 0)
        {
            var msg = LanguageHelper.ParseSet(Lang) + $"\n{string.Join("\n", set.InvalidLines)}";
            await ReplyAsync(msg).ConfigureAwait(false);
            return;
        }

        try
        {
            var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
            var sav = SaveUtil.GetBlankSAV(trainer.Version, trainer.OT);
            var pkm = sav.GetLegal(template, out var result);

            if (Info.Hub.Config.Legality.SetSuggestedMoves || pkm is PA8)
            {
                var movesClone = pkm.Clone();
                movesClone.SetSuggestedMoves();

                var originalMoves = new HashSet<ushort> { pkm.Move1, pkm.Move2, pkm.Move3, pkm.Move4 };
                var clonedMoves = new List<ushort> { movesClone.Move1, movesClone.Move2, movesClone.Move3, movesClone.Move4 };

                static ushort GetNonDuplicateMove(ushort originalMove, List<ushort> possibleMoves, HashSet<ushort> existingMoves)
                {
                    if (originalMove == 0)
                    {
                        foreach (var move in possibleMoves)
                        {
                            if (!existingMoves.Contains(move))
                            {
                                existingMoves.Add(move);
                                return move;
                            }
                        }
                    }
                    return originalMove;
                }

                pkm.Move1 = GetNonDuplicateMove(pkm.Move1, clonedMoves, originalMoves);
                pkm.Move2 = GetNonDuplicateMove(pkm.Move2, clonedMoves, originalMoves);
                pkm.Move3 = GetNonDuplicateMove(pkm.Move3, clonedMoves, originalMoves);
                pkm.Move4 = GetNonDuplicateMove(pkm.Move4, clonedMoves, originalMoves);
            }

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];

            if (pkm is not PB7)
            {
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
            }

            bool memes = Info.Hub.Config.Trade.MiscSettings.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
            if (memes)
                return;

            if (Info.Hub.Config.Legality.SetSuggestedRelearnMoves && pkm is not PA8 or PB7)
                pkm.SetRelearnMoves(la);

            if (Info.Hub.Config.Legality.SetAllTechnicalRecords)
                if (pkm is ITechRecord tr)
                    tr.SetRecordFlagsAll(la.Info.EvoChainsAllGens.Get(pkm.Context));

            if (pkm is not T pk || !la.Valid)
            {
                var reason = result switch
                {
                    "Timeout" => LanguageHelper.Timeout(Lang, spec),
                    "VersionMismatch" => LanguageHelper.Mismatch(Lang),
                    _ => LanguageHelper.Unable(Lang, spec)
                };
                var oops = LanguageHelper.Oops(Lang);
                var embed = new EmbedBuilder();
                var imsg = $"{Context.User.Mention} - {reason}";
                if (result == "Failed")
                    if (Info.Hub.Config.Legality.EnableHOMETrackerCheck)
                    {
                        if (pkm is PK9 && HomeTransfers.IsHomeTransferOnlySV((Species)pkm.Species, pkm.Form))
                        {
                            var ALMHint = $"{string.Format(BattleTemplateLegality.HOME_TRANSFER_ONLY, spec, "SV")}";
                            embed = LegalityReportEmbed(la, oops, imsg, ALMHint); 
                        }
                        else
                        {
                            var ALMHint = $"{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                            embed = LegalityReportEmbed(la, oops, imsg, ALMHint);
                        }
                    }
                    else
                    {
                        var ALMHint = $"{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                        embed = LegalityReportEmbed(la, oops, imsg, ALMHint);
                    }
                await ReplyAsync(embed : embed.Build()).ConfigureAwait(false);
                return;
            }

            pk.ResetPartyStats();

            var sig = Context.User.GetFavor();

            await AddTradeToQueueAsync(tcode, pk, sig, Context.User, lgcode, false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
            var msg = LanguageHelper.Unexpected(Lang) + $"\n```{string.Join("\n", set.GetSetLines())}```";
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }

    [Command("HiddenTrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncNoEmbed([Summary("Showdown Set")][Remainder] string content)
    {
        var code = Info.GetRandomTradeCode();
        await TradeAsyncNoEmbed(code, content).ConfigureAwait(false);
    }

    [Command("HiddenTrade")]
    [Alias("ht")]
    [Summary("Makes the bot trade you the attached file without displaying an embed is they are enabled.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsyncAttachNoEmbed()
    {
        var code = Info.GetRandomTradeCode();
        await TradeAsyncAttachNoEmbed(code).ConfigureAwait(false);
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

    private async Task SendLegalityReportEmbed(LegalityAnalysis la, PKM pkm, string msg)
    {
        var builder = new EmbedBuilder
        {
            Color = Color.Red,
            Title = LanguageHelper.LAReport(Lang),
            Description = $"{pkm.FileName}",
        };

        builder.AddField(x =>
        {
            x.Name = LanguageHelper.Invalid(Lang);
            x.Value = la.Report(false);
            x.IsInline = false;
        });

        await ReplyAsync(msg, embed: builder.Build()).ConfigureAwait(false);
    }

    private static EmbedBuilder LegalityReportEmbed(LegalityAnalysis la, string oops, string reason, string hint)
    {
        var builder = new EmbedBuilder
        {
            Color = Color.Red,
            Title = oops,
            Description = $"{reason}\n{hint}",
        };
        if (Info.Hub.Config.Legality.IncludeLegalityAnalysis && reason.Contains("Exhausted"))
        {
            builder.AddField(x =>
            {
                x.Name = LanguageHelper.Invalid(Lang);
                x.Value = $"```\n{la.Report(false)}```";
                x.IsInline = false;
            });
        }
        return builder;
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

        if (!Info.Hub.Config.Legality.AllowHOMETrackers && Info.Hub.Config.Legality.EnableHOMETrackerCheck && pk is IHomeTrack { HasTracker: true })
        {
            var msg = $"{usr.Mention}, I do not accept files with Home trackers!";
            await ReplyAsync(msg).ConfigureAwait(false);
            return;
        }

        await AddTradeToQueueAsync(code, pk, sig, usr, lgcode, displayEmbeds).ConfigureAwait(false);
    }

    private async Task AddTradeToQueueAsync(int code, T pk, RequestSignificance sig, SocketUser usr, List<PictoCodes> lgcode, bool displayEmbeds = true, bool userTrade = false)
    {
        var (canBeTraded, errorMessage) = pk.CanBeTraded();
        if (!canBeTraded)
        {
            await ReplyAsync($"{Context.User.Mention}, {errorMessage}").ConfigureAwait(false);
            return;
        }

        var la = new LegalityAnalysis(pk);
        if (!la.Valid)
        {
            await SendLegalityReportEmbed(la, pk, $"{Context.User.Mention}, {typeof(T).Name} attachment is not legal, and cannot be traded!").ConfigureAwait(false);
            return;
        }

        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr, lgcode, displayEmbeds, true, userTrade).ConfigureAwait(false);
    }
}