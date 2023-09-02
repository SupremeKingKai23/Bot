using System.ComponentModel;

namespace SysBot.Pokemon
{
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

        [Browsable(false)]
        public string ChannelOpenEmoji => string.IsNullOrEmpty(ChannelOpenIcon) ? "✅" : ChannelOpenIcon;
        [Category(Operation), Description("Sets and Override Open Channel emoji to look for in and to set Trade Channel Names")]
        public string? ChannelOpenIcon { get; set; }

        [Browsable(false)]
        public string ChannelOpenText => string.IsNullOrEmpty(ChannelOpenMessage) ? "Trade Bot is Online" : ChannelOpenMessage;
        [Category(Operation), Description("Sets and overrides default Online Bot Status Announcement Message")]
        public string? ChannelOpenMessage { get; set; }

        [Browsable(false)]
        public string ChannelClosedEmoji => string.IsNullOrEmpty(ChannelClosedIcon) ? "❌" : ChannelClosedIcon;
        [Category(Operation), Description("Sets and Override Closed Channel emoji to look for in and to set Trade Channel Names")]
        public string? ChannelClosedIcon { get; set; }

        [Browsable(false)]
        public string ChannelClosedText => string.IsNullOrEmpty(ChannelClosedMessage) ? "Trade Bot is Offline" : ChannelClosedMessage;
        [Category(Operation), Description("Sets and overrides default Offline Bot Status Announcement Message")]
        public string? ChannelClosedMessage { get; set; }

        [Category(Channels), Description("Channels with these IDs are monitored for Bot Staus Announcements when enabled.")]
        public RemoteControlAccessList MonitoredChannels { get; set; } = new();
    }
}
