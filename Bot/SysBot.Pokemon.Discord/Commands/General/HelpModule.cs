using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class HelpModule<T>(CommandService Service) : ModuleBase<SocketCommandContext> where T : PKM, new ()
{
    [Command("Help")]
    [Alias("h")]
    [Summary("Lists all available commands.")]
    public async Task HelpAsync()
    {
        var builder = new EmbedBuilder
        {
            Color = Color.Green,
            Description = "## These are the commands you can use:",
        };

        var mgr = SysCordSettings.Manager;
        var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
        var owner = app.Owner.Id;
        var uid = Context.User.Id;

        foreach (var module in Service.Modules.OrderBy(module => module.Name))
        {
            string? description = null;
            HashSet<string> mentioned = [];
            foreach (var cmd in module.Commands.OrderBy(cmd => cmd.Name))
            {
                var name = cmd.Name;
                if (mentioned.Contains(name))
                    continue;
                if (cmd.Attributes.Any(z => z is RequireOwnerAttribute) && owner != uid)
                    continue;
                if (cmd.Attributes.Any(z => z is RequireSudoAttribute) && !mgr.CanUseSudo(uid))
                    continue;
                if (module.Name == "SudoModule`1" && uid == owner)
                    continue;

                mentioned.Add(name);
                var result = await cmd.CheckPreconditionsAsync(Context).ConfigureAwait(false);
                if (result.IsSuccess)
                    description += $"{cmd.Aliases[0]}\n";
            }
            if (string.IsNullOrWhiteSpace(description))
                continue;

            var moduleName = module.Name;
            var gen = moduleName.IndexOf('`');
            if (gen != -1)
                moduleName = moduleName[..gen];

            builder.AddField(x =>
            {
                x.Name = moduleName;
                x.Value = description;
                x.IsInline = false;
            });
        }

        await ReplyAsync("Help has arrived!", false, builder.Build()).ConfigureAwait(false);
    }
    
    [Command("Help")]
    [Summary("Lists information about a specific command.")]
    public async Task HelpAsync([Summary("The command you want help for")] string command)
    {
        var result = Service.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.").ConfigureAwait(false);
            return;
        }

        var builder = new EmbedBuilder
        {
            Color = Color.Green,
            Description = $"## Here's what I found for \"__{command}__\"",
        };

        foreach (var match in result.Commands)
        {
            var cmd = match.Command;

            builder.AddField(x =>
            {
                x.Name = string.Join(", ", cmd.Aliases);
                x.Value = GetCommandSummary(cmd);
                x.IsInline = false;
            });
        }

        await ReplyAsync("Help has arrived!", false, builder.Build()).ConfigureAwait(false);
    }

    [Command("Commands")]
    [Alias("cmds")]
    [Summary("Lists available commands by modules in paged format.")]
    public async Task HelpPagedAsync()
    {
        var modules = Service.Modules.OrderBy(module => module.Name).ToList();
        var embeds = new List<Embed>();

        foreach (var module in modules)
        {
            var moduleName = module.Name;
            if (moduleName.Contains("`1"))
            {
                moduleName = moduleName.Replace("`1", string.Empty);
            }
            if (moduleName.Contains("Module"))
            {
                moduleName = moduleName.Replace("Module", string.Empty);
            }
            var builder = new EmbedBuilder
            {
                Color = Color.Green,
                Description = $"## Commands in the {moduleName} module:",
            };

            var commands = module.Commands.OrderBy(cmd => cmd.Name).ToList();
            HashSet<string> mentioned = [];
            EmbedBuilder? currentBuilder = null;
            int fieldCount = 0;

            foreach (var cmd in commands)
            {
                var name = cmd.Name;
                if (mentioned.Contains(name))
                    continue;
                if (cmd.Attributes.Any(z => z is RequireOwnerAttribute) && Context.Client.GetApplicationInfoAsync().Result.Owner.Id != Context.User.Id)
                    continue;
                if (cmd.Attributes.Any(z => z is RequireSudoAttribute) && !SysCordSettings.Manager.CanUseSudo(Context.User.Id))
                    continue;
                if (module.Name.Contains("Sudo") && Context.User.Id == Context.Client.GetApplicationInfoAsync().Result.Owner.Id)
                    continue;

                mentioned.Add(name);
                var result = cmd.CheckPreconditionsAsync(Context).Result;
                if (result.IsSuccess)
                {
                    if (currentBuilder == null || fieldCount >= 10)
                    {
                        if (currentBuilder != null)
                        {
                            embeds.Add(currentBuilder.Build());
                        }

                        currentBuilder = new EmbedBuilder
                        {
                            Color = Color.Green,
                            Description = fieldCount == 0 ?
                                $"## Commands in the {moduleName} module:" :
                                $"## Commands in the {moduleName} module(Cont'd):",
                        };
                        fieldCount = 0;
                    }

                    currentBuilder.AddField(x =>
                    {
                        x.Name = string.Join(", ", cmd.Aliases);
                        x.Value = GetCommandSummary(cmd);
                        x.IsInline = false;
                    });
                    fieldCount++;
                }
            }

            if (currentBuilder != null && currentBuilder.Fields.Count > 0)
            {
                embeds.Add(currentBuilder.Build());
            }
        }

        if (embeds.Count == 0)
        {
            await ReplyAsync("No commands available for you.").ConfigureAwait(false);
            return;
        }

        int currentPage = 0;
        var message = await ReplyAsync(embed: embeds[currentPage]).ConfigureAwait(false);
        await message.AddReactionAsync(new Emoji("⏪")).ConfigureAwait(false);
        await message.AddReactionAsync(new Emoji("⬅️")).ConfigureAwait(false);
        await message.AddReactionAsync(new Emoji("➡️")).ConfigureAwait(false);
        await message.AddReactionAsync(new Emoji("⏩")).ConfigureAwait(false);

        var lastReactionTime = DateTime.Now;
        async Task reactionHandler(SocketReaction reaction)
        {
            if (reaction.UserId == Context.User.Id && reaction.MessageId == message.Id)
            {
                if (reaction.Emote.Name == "⬅️" && currentPage > 0)
                {
                    currentPage--;
                    await message.ModifyAsync(msg => msg.Embed = embeds[currentPage]).ConfigureAwait(false);
                }
                else if (reaction.Emote.Name == "➡️" && currentPage < embeds.Count - 1)
                {
                    currentPage++;
                    await message.ModifyAsync(msg => msg.Embed = embeds[currentPage]).ConfigureAwait(false);
                }
                else if (reaction.Emote.Name == "⏪" && currentPage > 0)
                {
                    currentPage = 0;
                    await message.ModifyAsync(msg => msg.Embed = embeds[currentPage]).ConfigureAwait(false);
                    await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
                }
                else if (reaction.Emote.Name == "⏩" && currentPage < embeds.Count - 1)
                {
                    currentPage = embeds.Count - 1;
                    await message.ModifyAsync(msg => msg.Embed = embeds[currentPage]).ConfigureAwait(false);
                    await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
                }
                await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
                lastReactionTime = DateTime.Now;
            }
        }

        ExtraCommandUtil<T>.AddReactionHandler(message.Id, reactionHandler);

        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                if ((DateTime.Now - lastReactionTime).TotalSeconds > 20)
                {
                    ExtraCommandUtil<T>.RemoveReactionHandler(message.Id);
                    break;
                }
            }
        });

    }

    private static string GetCommandSummary(CommandInfo cmd)
    {
        return $"- {cmd.Summary}\n{(cmd.Parameters.Count == 0 ? "" : $" - {GetParameterSummary(cmd.Parameters)}")}";
    }

    private static string GetParameterSummary(IReadOnlyList<ParameterInfo> p)
    {
        return string.Join("\n - ", p.Select(GetParameterSummary));
    }

    private static string GetParameterSummary(ParameterInfo z)
    {
        var result = z.Name;
        if (!string.IsNullOrWhiteSpace(z.Summary))
            result += $" ({z.Summary})";
        return result;
    }
}