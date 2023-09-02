using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class GiveAwayResponse<T> where T : PKM, new()
    {
        public T Receive { get; }
        public GiveAwayResponseType Type { get; }

        public GiveAwayResponse(T pk, GiveAwayResponseType type)
        {
            Receive = pk;
            Type = type;
        }
    }
}