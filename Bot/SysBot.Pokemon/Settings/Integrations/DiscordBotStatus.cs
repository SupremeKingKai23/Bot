using System.ComponentModel;

namespace SysBot.Pokemon;

public class DiscordBotStatus
{
    private const string Operation = nameof(Operation);
    private const string Channels = nameof(Channels);
    public override string ToString() => "Discord Bot Status Settings";

    [Category(Operation), Description("Custom Status for playing a game.")]
    public string BotGameStatus { get; set; } = "SysBot.NET: Pokémon";

    [Category(Operation), Description("Indicates the Discord presence status color only considering bots that are Trade-type.")]
    public bool BotColorStatusTradeOnly { get; set; } = true;

    [Category(Operation), Description("When set to \"True\", The bot will send a Bot Status Announcement when Started or Stopped. ")]
    public bool StatusAnnouncements { get; set; } = false;

    [Category(Operation), Description("Use Thumnail Image instead of Large Embed image")]
    public bool UseThumbnailImage { get; set; } = false;

    [Category(Operation), Description("Provide an Image URL to be included in Bot Status Announcement when Open.")]
    public string ChannelOpenImgURL { get; set; } = string.Empty;

    [Category(Operation), Description("Sets an override Open Channel emoji to look for in and to set Trade Channel Names")]
    public string ChannelOpenIcon { get; set; } = string.Empty;

    [Category(Operation), Description("Sets an override Online Bot Status Announcement Message")]
    public string ChannelOpenMessage { get; set; } = string.Empty;

    [Category(Operation), Description("Provide an Image URL to be included in Bot Status Announcement when Closed.")]
    public string ChannelClosedImgURL { get; set; } = string.Empty;

    [Category(Operation), Description("Sets an override Closed Channel emoji to look for in and to set Trade Channel Names")]
    public string ChannelClosedIcon { get; set; } = string.Empty;

    [Category(Operation), Description("Sets and overrides default Offline Bot Status Announcement Message")]
    public string ChannelClosedMessage { get; set; } = string.Empty;

    [Category(Channels), Description("Channels with these IDs are monitored for Bot Staus Announcements when enabled.")]
    public RemoteControlAccessList MonitoredChannels { get; set; } = new();
}