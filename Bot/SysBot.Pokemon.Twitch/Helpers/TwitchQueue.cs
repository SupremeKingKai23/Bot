using PKHeX.Core;

namespace SysBot.Pokemon.Twitch;

public class TwitchQueue<T>(T pkm, PokeTradeTrainerInfo trainer, string username, bool subscriber) where T : PKM, new()
{
    public T Pokemon { get; } = pkm;
    public PokeTradeTrainerInfo Trainer { get; } = trainer;
    public string UserName { get; } = username;
    public string DisplayName => Trainer.TrainerName;
    public bool IsSubscriber { get; } = subscriber;
}