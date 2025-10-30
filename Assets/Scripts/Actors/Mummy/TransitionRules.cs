using System;
using System.Collections.Generic;
using static PlayerEnum;

/// <summary>
/// TransitionRules
/// Define todas las transiciones permitidas entre estados y mapea State->Action.
/// Fuente única de verdad para la matriz de estados (legible y auditable).
/// </summary>
public static class TransitionRules
{
    private static readonly IReadOnlyDictionary<PlayerStateId, PlayerStateId[]> _allowed =
        new Dictionary<PlayerStateId, PlayerStateId[]>
        {
            { PlayerStateId.Idle,        new[]{ PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Aim, PlayerStateId.Smash, PlayerStateId.DropBandage, PlayerStateId.Push, PlayerStateId.Attract, PlayerStateId.Swing, PlayerStateId.QuickTravel, PlayerStateId.Dead } },
            { PlayerStateId.Walk,        new[]{ PlayerStateId.Idle, PlayerStateId.Fall, PlayerStateId.Aim, PlayerStateId.Smash, PlayerStateId.DropBandage, PlayerStateId.Push, PlayerStateId.Attract, PlayerStateId.Swing, PlayerStateId.QuickTravel, PlayerStateId.Dead } },
            { PlayerStateId.Fall,        new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Swing, PlayerStateId.Dead } },
            { PlayerStateId.Aim,         new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Shoot, PlayerStateId.Dead } },
            { PlayerStateId.Shoot,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead} },
            { PlayerStateId.Smash,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.DropBandage, new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.Push,        new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.Attract,     new[]{ PlayerStateId.Idle, PlayerStateId.Dead } },
            { PlayerStateId.Swing,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.QuickTravel, new[]{ PlayerStateId.Idle, PlayerStateId.Dead } },
            { PlayerStateId.Dead,        Array.Empty<PlayerStateId>() },
        };

    public static bool Can(PlayerStateId from, PlayerStateId to) =>
        _allowed.TryGetValue(from, out var nexts) && Array.IndexOf(nexts, to) >= 0;
}