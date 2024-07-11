﻿using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class AutoLegalityExtensionsDiscord
{
    public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, ITrainerInfo sav, ShowdownSet set, BotLanguage lang)
    {
        if (set.Species <= 0)
        {
            await channel.SendMessageAsync("Oops! I wasn't able to interpret your message! If you intended to convert something, please double check what you're pasting!").ConfigureAwait(false);
            return;
        }

        try
        {
            var template = AutoLegalityWrapper.GetTemplate(set);
            var pkm = sav.GetLegal(template, out var result);
            if (pkm is PK8 && pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                TradeExtensions<PK8>.EggTrade(pkm, template);
            else if (pkm is PB8 && pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                TradeExtensions<PB8>.EggTrade(pkm, template);
            else if (pkm is PK9 && pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                TradeExtensions<PK9>.EggTrade(pkm, template);

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];
            if (!la.Valid)
            {
                var reason = result switch
                {
                    "Timeout" => LanguageHelper.Timeout(lang, spec),
                    "VersionMismatch" => LanguageHelper.Mismatch(lang),
                    _ => LanguageHelper.Unable(lang, spec)
                };
                var oops = lang switch
                {
                    BotLanguage.Español => "¡Ups",
                    _ => "Oops"
                };
                var imsg = $"{oops} {reason}";
                if (result == "Failed")
                    imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                await channel.SendMessageAsync(imsg).ConfigureAwait(false);
                return;
            }

            var msg = lang switch
            {

                _ => $"Here's your ({result}) legalized PKM for {spec} ({la.EncounterOriginal.Name})!"
            };
            await channel.SendPKMAsync(pkm, msg + $"\n{ReusableActions.GetFormattedShowdownText(pkm)}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(AutoLegalityExtensionsDiscord));
            var msg = lang switch
            {

                _ => $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```"
            };
            await channel.SendMessageAsync(msg).ConfigureAwait(false);
        }
    }

    public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, string content, byte gen, BotLanguage lang)
    {
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var sav = AutoLegalityWrapper.GetTrainerInfo(gen);
        await channel.ReplyWithLegalizedSetAsync(sav, set, lang).ConfigureAwait(false);
    }

    public static async Task ReplyWithLegalizedSetAsync<T>(this ISocketMessageChannel channel, string content, BotLanguage lang) where T : PKM, new()
    {
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        await channel.ReplyWithLegalizedSetAsync(sav, set, lang).ConfigureAwait(false);
    }

    public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, IAttachment att)
    {
        var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
        if (!download.Success)
        {
            await channel.SendMessageAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var pkm = download.Data!;
        if (new LegalityAnalysis(pkm).Valid)
        {
            await channel.SendMessageAsync($"{download.SanitizedFileName}: Already legal.").ConfigureAwait(false);
            return;
        }

        var legal = pkm.LegalizePokemon();
        if (!new LegalityAnalysis(legal).Valid)
        {
            await channel.SendMessageAsync($"{download.SanitizedFileName}: Unable to legalize.").ConfigureAwait(false);
            return;
        }

        legal.RefreshChecksum();

        var msg = $"Here's your legalized PKM for {download.SanitizedFileName}!\n{ReusableActions.GetFormattedShowdownText(legal)}";
        await channel.SendPKMAsync(legal, msg).ConfigureAwait(false);
    }
}