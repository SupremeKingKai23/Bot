using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Drawing.PokeSprite;
using SysBot.Pokemon.Discord.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using static PKHeX.Core.AutoMod.Aesthetics;
using static SysBot.Pokemon.Discord.Helpers.ColorHelper;
using static SysBot.Pokemon.Discord.Helpers.TypeHelper;
using Color = Discord.Color;

namespace SysBot.Pokemon.Discord
{
    public static class QueueHelper<T> where T : PKM, new()
    {
        private static EmbedBuilder Embed { get; set; } = new();
        private static string? ETA;
        private static string? EmbedMsg;
        private static string? Queuepos;
        private static QueueResultAdd? Added;
        private const uint MaxTradeCode = 9999_9999;
        private static SocketUser? Adder;

        public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser trader, List<PictoCodes> lgcode, bool displayEmbeds = true, bool displayInfo = true, bool userTrade = false)
        {
            Adder = context.User;
            if ((uint)code > MaxTradeCode)
            {
                await context.Channel.SendMessageAsync("Trade code should be 00000000-99999999!").ConfigureAwait(false);
                return;
            }

            try
            {
                const string helper = "I've added you to the queue! I'll message you here when your trade is starting.";
                var test = await trader.SendMessageAsync(helper).ConfigureAwait(false);
                var hub = SysCord<T>.Runner.Hub;

                // Try adding
                var result = AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, lgcode, out var msg, out var msg2, out var msg3);

                // Notify in channel
                if (hub.Config.Trade.UseTradeEmbeds && displayEmbeds)
                {
                    await AddToTradeQueueEmbed(trade, type, trader, displayInfo, userTrade);
                    await context.Channel.SendMessageAsync(embed: Embed.Build()).ConfigureAwait(false);
                }
                else if (!displayInfo)
                {
                    await context.Channel.SendMessageAsync(msg3).ConfigureAwait(false);
                }
                else
                {
                    await context.Channel.SendMessageAsync(msg).ConfigureAwait(false);
                }
                // Notify in PM to mirror what is said in the channel.
                if (trade is PB7)
                {
                    var (thefile, lgcodeembed) = CreateLGLinkCodeSpriteEmbed(lgcode);
                    await trader.SendFileAsync(thefile, $"{msg}\nYour trade code will be.", embed: lgcodeembed).ConfigureAwait(false);
                }
                else
                {
                    await trader.SendMessageAsync($"{(displayInfo ? msg : msg3)}\nYour trade code will be: {code:0000 0000}");
                }

                // Clean Up
                if (result)
                {
                    // Delete the user's join message for privacy
                    if (!context.IsPrivate)
                        await context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
                }
                else
                {
                    // Delete our "I'm adding you!", and send the same message that we sent to the general channel.
                    await test.DeleteAsync().ConfigureAwait(false);
                }
            }
            catch (HttpException ex)
            {
                await HandleDiscordExceptionAsync(context, (trader as IGuildUser)!, ex).ConfigureAwait(false);
            }
        }

        public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser usr, List<PictoCodes> lgcode, int catchID = 0)
        {
            await AddToQueueAsync(context, code, trainer, sig, trade, routine, type, usr , lgcode, true, false).ConfigureAwait(false);
        }

        private static bool AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName, RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, List<PictoCodes> lgcode, out string msg, out string msg2, out string msg3)
        {
            var userID = trader.Id;
            var name = trader.Username;

            var trainer = new PokeTradeTrainerInfo(trainerName, userID);
            var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, trader, lgcode);
            var detail = new PokeTradeDetail<T>(pk, trainer, notifier, t, code, lgcode, sig == RequestSignificance.Favored);
            var trade = new TradeEntry<T>(detail, userID, type, name);
            var mgr = SysCordSettings.Manager;
            var hub = SysCord<T>.Runner.Hub;
            var Info = hub.Queues.Info;

            Added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

            if (Added == QueueResultAdd.AlreadyInQueue && !mgr.CanUseSudo(trader.Id))
            {
                msg = msg2 = msg3 = "Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userID, type);

            var ticketID = "";
            if (TradeStartModule<T>.IsStartChannel(context.Channel.Id))
                ticketID = $", unique ID: {detail.ID}";

            var pokeName = "";
            var pokeName2 = "";
            if ((t == PokeTradeType.Specific || t == PokeTradeType.SupportTrade || t == PokeTradeType.Giveaway) && pk.Species != 0)
            {
                pokeName = $"{(hub.Config.Trade.UseTradeEmbeds ? "" : t == PokeTradeType.SupportTrade && pk.Species != (int)Species.Ditto && pk.HeldItem != 0 ? $" {GameInfo.GetStrings(1).Species[pk.Species]} delivering {(ArticleChoice(GameInfo.GetStrings(1).Item[pk.HeldItem][1]) ? "an" : "a")} {GameInfo.GetStrings(1).Item[pk.HeldItem]}" : $" Receiving: {GameInfo.GetStrings(1).Species[pk.Species]}. ")}";
                pokeName2 = $" {(t == PokeTradeType.SupportTrade && pk.Species != (int)Species.Ditto && pk.HeldItem != 0 ? $"{GameInfo.GetStrings(1).Species[pk.Species]} delivering {(ArticleChoice(GameInfo.GetStrings(1).Item[pk.HeldItem][0]) ? "an" : "a")} {GameInfo.GetStrings(1).Item[pk.HeldItem]}" : t == PokeTradeType.Random ? "" : $"Receiving: {GameInfo.GetStrings(1).Species[pk.Species]}")}. ";
            }
            msg = $"{trader.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}.{pokeName2}";
            msg2 = $"{(hub.Config.Trade.UseAlternateLayout ? "" : $"{trader.Mention} - ")}Added to the {type} queue{ticketID}. ";
            msg3 = $"{trader.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}.";

            EmbedMsg = msg2;
            Queuepos = $"Current Position: {position.Position}.{pokeName}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                ETA = $"{(hub.Config.Trade.UseTradeEmbeds ? "" : "\n")}Estimated Wait: {eta:F1} minutes.";
                msg += ETA;
            }
            return true;
        }

        private static Task AddToTradeQueueEmbed(T pk, PokeTradeType type, SocketUser trader, bool notMysteryegg, bool mentionTrade)
        {
            var hub = SysCord<T>.Runner.Hub;
            if (notMysteryegg && type is not PokeTradeType.Clone)
            {
                var name = string.Empty;
                try
                {
                    name = NicknameHelper.Get(trader as IGuildUser);
                }
                catch 
                {
                    name = trader.Username;
                }   
                var ballUrl = $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{((Ball)pk.Ball).ToString().ToLower()}ball.png";
                var strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
                var items = strings.GetItemStrings(pk.Context, (GameVersion)pk.Version);
                var formName = ShowdownParsing.GetStringFromForm(pk.Form, strings, pk.Species, pk.Context);
                var formStrings = FormConverter.GetFormArgumentStrings(pk.Species);
                var pkarg = pk as IFormArgument;
                var formArg = string.Empty;
                if (pkarg is not null && pk.Species is (ushort)Species.Alcremie)
                    formArg = formStrings[pkarg.FormArgument];
                var EggURL = $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Sprites/128x128/Egg_{GetPrimaryType(TypeMap[(Species)pk.Species])}.png";
                var itemName = items[pk.HeldItem];
                var itemURL = $"https://genpkm.com/sprites/itemdex/{itemName.Replace(" ", "").ToLower()}.png";
                var movesList = "";
                for (int i = 0; i < pk.Moves.Length; i++)
                {
                    if (pk.Moves[i] != 0)
                    {
                        movesList += $"{(hub.Config.Trade.UseAlternateLayout ? "\\- " : "- ")}{GameInfo.GetStrings(1).Move[pk.Moves[i]]}\n";
                    }
                }
                var author = new EmbedAuthorBuilder
                {
                    Name = $"{name}'s{(pk.IsShiny ? " Shiny" : "")} {(pk.IsNicknamed && pk.Nickname != "Egg" ? $"{pk.Nickname}" : $"Pokémon")} {(pk.IsEgg ? "Egg" : "")}",
                    IconUrl = hub.Config.Trade.UseAlternateLayout ? pk.HeldItem != 0 ? ballUrl : trader.GetAvatarUrl() : ballUrl,
                    Url = "https://caterpie.net/"
                };
                var footer = new EmbedFooterBuilder
                {
                    Text = Queuepos + "\n" + ETA,
                };                
                Embed = new EmbedBuilder
                {
                    Color = GetDiscordColor(pk.IsShiny ? ShinyMap[((Species)pk.Species, pk.Form)] : (PersonalColor)pk.PersonalInfo.Color),
                    ThumbnailUrl = hub.Config.Trade.UseAlternateLayout ? pk.HeldItem != 0 ? itemURL: ballUrl : pk.IsEgg ? EggURL : TradeExtensions<PK9>.PokeImg(pk, false, false),
                    Author = author,
                    Footer = footer,
                };
                Embed.AddField(x =>
                {
                    x.Name = Format.Bold($"{(pk.IsShiny ? "★ " : "")}{GameInfo.GetStrings(1).Species[pk.Species]}{(pk.Species == (ushort)Species.Alcremie ? $"-{formArg}" : "")}{(pk.Form != 0 ? $"-{formName}" : "")}{(pk.Gender == 0 ? " (M)" : pk.Gender == 1 ? " (F)" : "")} {(pk.HeldItem != 0 ? hub.Config.Trade.UseAlternateLayout ? "" : $"➜ {itemName}" : "")}");
                    x.Value =
                    $"Ability: {GameInfo.GetStrings(1).Ability[pk.Ability]}\n" +
                    $"{(pk is PK9 pk9 ? pk9.TeraTypeOverride <= MoveType.Fairy ? $"Tera Type: {pk9.TeraTypeOverride}\n" : $"Tera Type: {pk9.TeraTypeOriginal}\n" : "")}" +
                    $"{(pk is PK8 pk8 ? pk8.CanGigantamax ? $"Gigantamax: Yes\n" : "" : "")}" +
                    $"{(pk is PA8 pa8 ? pa8.IsAlpha ? $"Alpha: Yes\n" : "" : "")}" +
                    $"{(pk.IsEgg ? "" : $"Level: {pk.CurrentLevel}\n")}" +
                    $"{(Nature)pk.Nature} Nature\n" +
                    $"{(pk is PA8 pa8b ? $"GVs: {pa8b.GV_HP}/{pa8b.GV_ATK}/{pa8b.GV_DEF}/{pa8b.GV_SPA}/{pa8b.GV_SPD}/{pa8b.GV_SPE}\n" : $"IVs: {pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}\n")}" +
                    $"{(pk.EVTotal >= 1 ? $"EVs: {GetEVString(pk)}\n" : "")}" +
                    $"{(hub.Config.Trade.UseAlternateLayout ? "" : $"{movesList}\n{(mentionTrade ? $"{EmbedMsg}\nAdded by {Adder.Mention}" : $"{EmbedMsg}")}")}";
                    x.IsInline = true;
                });
                if (hub.Config.Trade.UseAlternateLayout)
                {
                    Embed.AddField(x =>
                    {
                        x.Name = "Moves:";
                        x.Value = movesList;
                        x.IsInline = true;
                    });
                    Embed.AddField(x =>
                    {
                        x.Name = "\u200b";
                        x.Value = $"{trader.Mention}\n{(mentionTrade ? $"{EmbedMsg}\nAdded by {Adder.Mention}" : $"{EmbedMsg}")}";
                        x.IsInline = false;
                    });
                    Embed.WithImageUrl(pk.IsEgg ? EggURL : TradeExtensions<PK9>.PokeImg(pk, false, false));
                }

                ETA = "";
                Adder = null;
                return Task.CompletedTask;
            }
            if (notMysteryegg && type is PokeTradeType.Clone)
            {
                var name = string.Empty;
                try
                {
                    name = NicknameHelper.Get(trader as IGuildUser);
                }
                catch
                {
                    name = trader.Username;
                }               
                var author = new EmbedAuthorBuilder
                {
                    Name = "Cloning Pod Activated",
                    IconUrl =  trader.GetAvatarUrl(),
                    Url = "https://caterpie.net/"
                };
                var footer = new EmbedFooterBuilder
                {
                    Text = Queuepos + "\n" + ETA,
                };
                Embed = new EmbedBuilder
                {
                    Color = Color.Green,
                    ThumbnailUrl = "https://i.imgur.com/bkxt4JJ.png",
                    ImageUrl = "https://i.imgur.com/7L5CfPt.png",
                    Author = author,
                    Footer = footer,
                };
                Embed.AddField(x =>
                {
                    x.Name = "\u200b";
                    x.Value = $"{trader.Mention}\n{(mentionTrade ? $"{EmbedMsg}\nAdded by {Adder.Mention}" : !hub.Config.Trade.UseAlternateLayout ? $"{EmbedMsg.Split("-")[1].Trim()}" : $"{EmbedMsg}")}";
                    x.IsInline = false;
                });

                ETA = "";
                Adder = null;
                return Task.CompletedTask;
            }
            else
            {
                var name = NicknameHelper.Get(trader as IGuildUser);
                var ballUrl = $"https://raw.githubusercontent.com/BakaKaito/HomeImages/main/Ballimg/50x50/{((Ball)pk.Ball).ToString().ToLower()}ball.png";
                var author = new EmbedAuthorBuilder
                {
                    Name = $"{name}'s Mystery Egg",
                    IconUrl = hub.Config.Trade.UseAlternateLayout ? trader.GetAvatarUrl() : ballUrl,
                    Url = "https://caterpie.net/"
                };
                var footer = new EmbedFooterBuilder
                {
                    Text = Queuepos + "\n" + ETA,
                };
                var EggURL = $"https://raw.githubusercontent.com/BakaKaito/HomeImages/Home3.0/Sprites/128x128/MysteryEgg.png";

                Embed = new EmbedBuilder
                {
                    Color = GetDiscordColor(pk.IsShiny ? ShinyMap[((Species)pk.Species, pk.Form)] : (PersonalColor)pk.PersonalInfo.Color),
                    ThumbnailUrl = hub.Config.Trade.UseAlternateLayout ? ballUrl : EggURL,
                    Author = author,
                    Footer = footer,
                };
                Embed.AddField(x =>
                {
                    x.Name = $"*Unknown*";
                    x.Value = $"Ability: *Unknown*\nLevel: 1\n *Unknown* Nature\nIVs: ??/??/??/??/??/??\n{(hub.Config.Trade.UseAlternateLayout ? "" : $"Moves:\n- *Unknown*\n- *Unknown*\n- *Unknown*\n- *Unknown*\n\n{EmbedMsg}")}";
                    x.IsInline = true;
                });
                if (hub.Config.Trade.UseAlternateLayout)
                {
                    Embed.AddField(x =>
                    {
                        x.Name = "Moves:";
                        x.Value = "\\- *Unknown*\n\\- *Unknown*\n\\- *Unknown*\n\\- *Unknown*";
                        x.IsInline = true;
                    });
                    Embed.AddField(x =>
                    {
                        x.Name = "\u200b";
                        x.Value = $"{trader.Mention}\n{EmbedMsg}";
                        x.IsInline = false;
                    });
                    Embed.WithImageUrl(EggURL);
                }

                ETA = "";
                Adder = null;
                return Task.CompletedTask;
            }
        }

        public static bool ArticleChoice(char letter)
        {
            letter = char.ToLowerInvariant(letter);
            return letter switch
            {
                'a' or 'e' or 'i' or 'o' or 'u' or 'y' => true,
                _ => false,
            };

        }

        private static string GetEVString(T pk)
        {
            List<string> evList = new List<string>();

            if (pk.EV_HP > 0)
                evList.Add($"{pk.EV_HP} HP");
            if (pk.EV_ATK > 0)
                evList.Add($"{pk.EV_ATK} Atk");
            if (pk.EV_DEF > 0)
                evList.Add($"{pk.EV_DEF} Def");
            if (pk.EV_SPA > 0)
                evList.Add($"{pk.EV_SPA} SpA");
            if (pk.EV_SPD > 0)
                evList.Add($"{pk.EV_SPD} SpD");
            if (pk.EV_SPE > 0)
                evList.Add($"{pk.EV_SPE} Spe");

            return string.Join(" / ", evList);
        }

        private static async Task HandleDiscordExceptionAsync(SocketCommandContext context, IGuildUser trader, HttpException ex)
        {
            var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var owner = app.Owner.Id;
            string message = string.Empty;
            EmbedBuilder embedBuilder = new();
            switch (ex.DiscordCode)
            {
                case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
                    {
                        // Check if the exception was raised due to missing "Send Messages" or "Manage Messages" permissions. Nag the bot owner if so.
                        var permissions = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);
                        if (!permissions.SendMessages)
                        {
                            // Nag the owner in logs.
                            message = "You must grant me \"Send Messages\" permissions!";
                            Base.LogUtil.LogError(message, "QueueHelper");
                            return;
                        }
                        if (!permissions.ManageMessages)
                        {
                            message = "I must be granted \"Manage Messages\" permissions!";
                            embedBuilder.Title = "Permissions Error";
                        }
                    }
                    break;
                case DiscordErrorCode.CannotSendMessageToUser:
                    {
                        // The user either has DMs turned off, or Discord thinks they do.
                        message = context.User == trader ? $"{context.User.Mention}\nYou must enable Direct Messages in order to be queued!" : "The mentioned user must enable private messages in order for them to be queued!";
                        embedBuilder.Title = "Privacy Error";
                    }
                    break;
                default:
                    {
                        // Send a generic error message.
                        message = ex.DiscordCode != null ? $"Discord error {(int)ex.DiscordCode}: {ex.Reason}" : $"Http error {(int)ex.HttpCode}: {ex.Message}";
                    }
                    break;
            }
            embedBuilder.Description = message;
            embedBuilder.Color = Color.Red;
            embedBuilder.ThumbnailUrl = context.Client.CurrentUser.GetAvatarUrl();
            var pingOwner = ex.DiscordCode == (DiscordErrorCode.InsufficientPermissions | DiscordErrorCode.MissingPermissions);
            var embed = embedBuilder.Build();
            await context.Message.ReplyAsync(pingOwner ? $"<@{owner}>" : "", false, embed: embed).ConfigureAwait(false);
        }

        public static (string, Embed) CreateLGLinkCodeSpriteEmbed(List<PictoCodes> lgcode)
        {
            int codecount = 0;
            List<System.Drawing.Image> spritearray = new();
            foreach (PictoCodes cd in lgcode)
            {

                var showdown = new ShowdownSet(cd.ToString());
                var sav = SaveUtil.GetBlankSAV(EntityContext.Gen7b, "pip");
                PKM pk = sav.GetLegalFromSet(showdown, out _);
                System.Drawing.Image png = pk.Sprite();
                var destRect = new Rectangle(-40, -65, 137, 130);
                var destImage = new Bitmap(137, 130);

                destImage.SetResolution(png.HorizontalResolution, png.VerticalResolution);

                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.DrawImage(png, destRect, 0, 0, png.Width, png.Height, GraphicsUnit.Pixel);

                }
                png = destImage;
                spritearray.Add(png);
                codecount++;
            }
            int outputImageWidth = spritearray[0].Width + 20;

            int outputImageHeight = spritearray[0].Height - 65;

            Bitmap outputImage = new(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.DrawImage(spritearray[0], new Rectangle(0, 0, spritearray[0].Width, spritearray[0].Height),
                    new Rectangle(new Point(), spritearray[0].Size), GraphicsUnit.Pixel);
                graphics.DrawImage(spritearray[1], new Rectangle(50, 0, spritearray[1].Width, spritearray[1].Height),
                    new Rectangle(new Point(), spritearray[1].Size), GraphicsUnit.Pixel);
                graphics.DrawImage(spritearray[2], new Rectangle(100, 0, spritearray[2].Width, spritearray[2].Height),
                    new Rectangle(new Point(), spritearray[2].Size), GraphicsUnit.Pixel);
            }
            System.Drawing.Image finalembedpic = outputImage;
            var filename = $"{System.IO.Directory.GetCurrentDirectory()}//TradeCode.png";
            finalembedpic.Save(filename);
            filename = System.IO.Path.GetFileName($"{System.IO.Directory.GetCurrentDirectory()}//TradeCode.png");
            Embed returnembed = new EmbedBuilder().WithTitle($"{lgcode[0]}, {lgcode[1]}, {lgcode[2]}").WithImageUrl($"attachment://{filename}").Build();
            return (filename, returnembed);
        }
    }
}
