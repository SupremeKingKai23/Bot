using PKHeX.Core;

namespace SysBot.Pokemon.Discord.Helpers;

public static class IPBotHelper<T> where T : PKM, new()
{
    public static string Get(PokeBotRunner<T> runner, string? ip = null)
    {
        if (ip != null) return ip;
        var bot = runner.Bots.Find(_ => true);

        return bot == null ? "127.0.0.1" : bot.Bot.Config.Connection.IP;
    }
}