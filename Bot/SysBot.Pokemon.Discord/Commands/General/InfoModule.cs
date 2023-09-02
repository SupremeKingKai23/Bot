using Discord;
using Discord.Commands;
using PKHeX.Core;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    // src: https://github.com/foxbot/patek/blob/master/src/Patek/Modules/InfoModule.cs
    // ISC License (ISC)
    // Copyright 2017, Christopher F. <foxbot@protonmail.com>
    public class InfoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private const string dev = "https://discord.gg/mergebot";
        private const string Version = "4.1";

        [Command("info")]
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
                $"- {Format.Bold("MergeBot Version")}: {Version}\n" +
                $"- {Format.Bold("PKHeX.Core Version")}: {GetVersionInfo("PKHeX.Core")}\n" +
                $"- {Format.Bold("AutoLegality Version")}: {GetVersionInfo("PKHeX.Core.AutoMod")}\n"+
                $"- {Format.Bold("Dev Server")}: [MergeBot Central]({dev})" 
                );

            await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
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
}
