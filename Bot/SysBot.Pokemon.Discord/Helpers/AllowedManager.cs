using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class AllowedManager
{
    public static async Task<bool> BlacklistedUser(ulong? userId = null)
    {
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var client = new HttpClient();
        var allowedList = await client.GetStringAsync($"https://listromago.s3.us-east-1.amazonaws.com/MENPK1YQ65G6XD4L80G5/Q9ZWDQ62NJ.json?t={time}");
        var list = JArray.Parse(allowedList).Children().Select(x => (ulong)x).ToArray();
        return list.Contains(userId ?? 0);
    }

    public static async Task<bool> BlacklistedServer(ulong? guildId = null)
    {
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var client = new HttpClient();
        var allowedList = await client.GetStringAsync($"https://listromago.s3.us-east-1.amazonaws.com/PE3M5XZ969RO4KRLVJQ1/K0R648OZLP.json?t={time}");
        var list = JArray.Parse(allowedList).Children().Select(x => (ulong)x).ToArray();
        return list.Contains(guildId ?? 0);
    }

    public static async Task<bool> BlacklistedBot(ulong? userID = null)
    {
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var client = new HttpClient();
        var allowedList = await client.GetStringAsync($"https://listromago.s3.us-east-1.amazonaws.com/PE3M5XZ969RO4KRLVJQ1/K0R648OZLP.json?t={time}");
        var list = JArray.Parse(allowedList).Children().Select(x => (ulong)x).ToArray();
        return list.Contains(userID ?? 0);
    }
}