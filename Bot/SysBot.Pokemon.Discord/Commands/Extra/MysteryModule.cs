using Discord.Commands;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues for Mystery Pokémon.")]
public class MysteryModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private static BotLanguage Lang => Info.Hub.Config.CurrentLanguage;

    [Command("MysteryEgg")]
    [Alias("me", "randomegg", "re")]
    [Summary("Makes the bot trade you an egg of a random Pokemon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task MysteryEggTradeAsync()

    {
        var code = Info.GetRandomTradeCode();
        await MysteryEggTradeAsync(code).ConfigureAwait(false);
    }

    [Command("MysteryEgg")]
    [Alias("me", "randomegg", "re")]
    [Summary("Makes the bot trade you an egg of a random Pokemon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task MysteryEggTradeAsync([Summary("Trade Code")] int code)
    {
        if (typeof(T) != typeof(PA8) && typeof(T) != typeof(PB7))
        {
            _ = TradeExtensions<T>.MysteryEgg(out var pk);
            var sig = Context.User.GetFavor();
            var lgcode = Info.GetRandomLGTradeCode();
            await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true, false).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync($"{(typeof(T) == typeof(PA8) ? "PLA" : "LGPE")}" + LanguageHelper.NoBreed(Lang)).ConfigureAwait(false);
        }
    }

    [Command("MysteryTrade")]
    [Alias("sp", "st", "surprisemon", "randommon", "suprise", "mt")]
    [Summary("Makes the bot trade you a random Pokemon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task MysteryMonTradeAsync()

    {
        var code = Info.GetRandomTradeCode();
        await MysteryMonTradeAsync(code).ConfigureAwait(false);
    }

    [Command("MysteryTrade")]
    [Alias("sp", "st", "surprisemon", "randommon", "suprise", "mt")]
    [Summary("Makes the bot trade you a random Pokemon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task MysteryMonTradeAsync([Summary("Trade Code")] int code)
    {
        bool foundValidSpecies = false;
        Species randomSpecies = Species.None;
        while (!foundValidSpecies)
        {
            Random random = new();
            int randomIndex = random.Next(0, (typeof(T) == typeof(PB7) ? TradeExtensions<T>.LGPE : typeof(T) == typeof(PK8) ? TradeExtensions<T>.SWSH : typeof(T) == typeof(PB8) ? TradeExtensions<T>.BDSP : typeof(T) == typeof(PA8) ? TradeExtensions<T>.LA : TradeExtensions<T>.SV).Length);
            int randomInt = (typeof(T) == typeof(PB7) ? TradeExtensions<T>.LGPE : typeof(T) == typeof(PK8) ? TradeExtensions<T>.SWSH : typeof(T) == typeof(PB8) ? TradeExtensions<T>.BDSP : typeof(T) == typeof(PA8) ? TradeExtensions<T>.LA : TradeExtensions<T>.SV)[randomIndex];
            randomSpecies = (Species)randomInt;

            var sav2 = AutoLegalityWrapper.GetTrainerInfo<T>();
            var set2 = new ShowdownSet(randomSpecies.ToString());
            var pkm2 = sav2.GetLegal(set2, out _);
            var la = new LegalityAnalysis(pkm2);

            if (la.Valid)
            {
                foundValidSpecies = true;
            }
        }

        var content = randomSpecies.ToString();
        Random random2 = new();
        var max = Info.Hub.Config.Trade.MiscSettings.MysteryTradeShinyOdds < 0 ? 0 : Info.Hub.Config.Trade.MiscSettings.MysteryTradeShinyOdds;
        int randomNumber = random2.Next(0, max);
        var shiny = randomNumber == 0;
        content += $"\n.IVs=$rand\n.Nature=$0,24\n{(shiny ? "Shiny: Yes\n" : "")}.Moves=$suggest\n.AbilityNumber=$0,2\n.TeraTypeOverride=$rand\n.Ball=$0,37\n.DynamaxLevel=$0,10\n.TrainerTID7=$0001,3559\n.TrainerSID7=$000001,993401\n.OriginalTrainerName=Surprise!\n.GV_ATK=$0,7\n.GV_DEF=$0,7\n.GV_HP=$0,7\n.GV_SPA=$0,7\n.GV_SPD=$0,7\n.GV_SPE=$0,7";

        var lgcode = Info.GetRandomLGTradeCode();
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        var pkm = sav.GetLegal(template, out _);
        if (set.InvalidLines.Count != 0)
        {
            var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
            await ReplyAsync(msg).ConfigureAwait(false);
            return;
        }

        while (true)
        {
            var la2 = new LegalityAnalysis(pkm);

            if (pkm is not T pk || !la2.Valid)
            {
                pkm = sav.GetLegal(template, out _);
                continue;
            }
            else
            {
                pk.ResetPartyStats();
                var sig = Context.User.GetFavor();
                await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.SupportTrade, Context.User, lgcode, true, true).ConfigureAwait(false);
                break;
            }
        }
    }
}