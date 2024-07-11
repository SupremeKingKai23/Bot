using PKHeX.Core;

namespace SysBot.Pokemon;

public class LedyResponse<T>(T pk, LedyResponseType type) where T : PKM, new()
{
    public T Receive { get; } = pk;
    public LedyResponseType Type { get; } = type;
}