using System;
using System.Collections.Generic;
using static PlayerEnum;

/// <summary> 
/// Matriz de Transiciones: Establece el mapa de conexiones permitidas entre estados (A -> B), 
/// funcionando como la "fuente de verdad" para la lógica de flujo de la State Machine. 
/// </summary>

public static class TransitionRules
{
    private static readonly IReadOnlyDictionary<PlayerStateId, PlayerStateId[]> _allowed =
        new Dictionary<PlayerStateId, PlayerStateId[]>
        {
            { PlayerStateId.Idle,        new[]{ PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Aim, PlayerStateId.Smash, PlayerStateId.DropBandage, PlayerStateId.Push, PlayerStateId.Attract, PlayerStateId.Swing, PlayerStateId.QuickTravel, PlayerStateId.KnockBack, PlayerStateId.Fake, PlayerStateId.Win,PlayerStateId.Dead } },
            { PlayerStateId.Walk,        new[]{ PlayerStateId.Idle, PlayerStateId.Fall, PlayerStateId.Aim, PlayerStateId.Smash, PlayerStateId.DropBandage, PlayerStateId.Push, PlayerStateId.Attract, PlayerStateId.Swing, PlayerStateId.QuickTravel, PlayerStateId.KnockBack, PlayerStateId.Fake, PlayerStateId.Win,PlayerStateId.Dead } },
            { PlayerStateId.Fall,        new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Swing, PlayerStateId.KnockBack, PlayerStateId.Dead } },
            { PlayerStateId.Aim,         new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Shoot, PlayerStateId.KnockBack, PlayerStateId.Dead } },
            { PlayerStateId.Shoot,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.KnockBack, PlayerStateId.Dead} },
            { PlayerStateId.Smash,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.KnockBack, PlayerStateId.Dead } },
            { PlayerStateId.DropBandage, new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.KnockBack, PlayerStateId.Dead } },
            { PlayerStateId.Push,        new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.KnockBack, PlayerStateId.Dead } },
            { PlayerStateId.Attract,     new[]{ PlayerStateId.Idle, PlayerStateId.KnockBack, PlayerStateId.Dead } },
            { PlayerStateId.Swing,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.KnockBack, PlayerStateId.Dead } },
            { PlayerStateId.QuickTravel, new[]{ PlayerStateId.Idle, PlayerStateId.Dead } },
            { PlayerStateId.KnockBack,   new[]{ PlayerStateId.Idle, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.Fake,        new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Win, PlayerStateId.Dead } },
            { PlayerStateId.Dead,        Array.Empty<PlayerStateId>() },
            { PlayerStateId.Win,        Array.Empty<PlayerStateId>() },
        };

    public static bool Can(PlayerStateId from, PlayerStateId to) =>
        _allowed.TryGetValue(from, out var nexts) && Array.IndexOf(nexts, to) >= 0;
}