using System;

namespace SysBot.Pokemon.Discord;

public class ChannelAction<T1, T2>(ulong id, Action<T1, T2> messager, string channel)
{
    public readonly ulong ChannelID = id;
    public readonly string ChannelName = channel;
    public readonly Action<T1, T2> Action = messager;
}