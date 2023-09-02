using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class GiveAwayRequest<T> where T : PKM, new()
    {
        public readonly string Nickname;
        public readonly T RequestInfo;

        public GiveAwayRequest(T requestInfo, string nickname)
        {
            RequestInfo = requestInfo;
            Nickname = nickname;
        }
    }
}