﻿using PKHeX.Core;

namespace SysBot.Pokemon;

public sealed class BotFactory9SV : BotFactory<PK9>
{
    public override PokeRoutineExecutorBase CreateBot(PokeTradeHub<PK9> Hub, PokeBotState cfg) => cfg.NextRoutineType switch
    {
        PokeRoutineType.FlexTrade or PokeRoutineType.Idle
            or PokeRoutineType.LinkTrade
            or PokeRoutineType.Clone
            or PokeRoutineType.Dump
            or PokeRoutineType.FixOT
            or PokeRoutineType.SpecialRequest
            => new PokeTradeBotSV(Hub, cfg),

        PokeRoutineType.RemoteControl => new RemoteControlBotSV(cfg),

        _ => throw new ArgumentException(nameof(cfg.NextRoutineType)),
    };

    public override bool SupportsRoutine(PokeRoutineType type) => type switch
    {
        PokeRoutineType.FlexTrade or PokeRoutineType.Idle
            or PokeRoutineType.LinkTrade
            or PokeRoutineType.Clone
            or PokeRoutineType.Dump
            or PokeRoutineType.FixOT
            or PokeRoutineType.SpecialRequest
            => true,

        PokeRoutineType.RemoteControl => true,

        _ => false,
    };
}