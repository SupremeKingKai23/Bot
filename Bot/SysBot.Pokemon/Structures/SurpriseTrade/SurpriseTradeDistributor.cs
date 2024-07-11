using PKHeX.Core;

namespace SysBot.Pokemon;

public class SurpriseTradeDistributor<T> where T : PKM, new()
{
    public readonly Dictionary<string, SurpriseTradeRequest<T>> SurpriseTrade;
    public readonly PokemonSTPool<T> Pool;

    public SurpriseTradeDistributor(PokemonSTPool<T> STpool)
    {
        Pool = STpool;
        SurpriseTrade = Pool.Files;
    }
}