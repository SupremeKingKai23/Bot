using PKHeX.Core;

namespace SysBot.Pokemon;

public class LedyRequest<T>(T requestInfo, string nickname) where T : PKM, new()
{
    public readonly string Nickname = nickname;
    public readonly T RequestInfo = requestInfo;
}