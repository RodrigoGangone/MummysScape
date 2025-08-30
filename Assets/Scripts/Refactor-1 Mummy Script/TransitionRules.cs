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
            { PlayerStateId.Idle,        new[]{ PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Shoot, PlayerStateId.Smash, PlayerStateId.DropBandage, PlayerStateId.Push, PlayerStateId.Attract, PlayerStateId.Swing, PlayerStateId.Dead } },
            { PlayerStateId.Walk,        new[]{ PlayerStateId.Idle, PlayerStateId.Fall, PlayerStateId.Shoot, PlayerStateId.Smash, PlayerStateId.DropBandage, PlayerStateId.Push, PlayerStateId.Attract, PlayerStateId.Swing, PlayerStateId.Dead } },
            { PlayerStateId.Fall,        new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Swing, PlayerStateId.Dead } },
            { PlayerStateId.Shoot,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.Smash,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.DropBandage, new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.Push,        new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.Attract,     new[]{ PlayerStateId.Idle, PlayerStateId.Dead } },
            { PlayerStateId.Swing,       new[]{ PlayerStateId.Idle, PlayerStateId.Walk, PlayerStateId.Fall, PlayerStateId.Dead } },
            { PlayerStateId.Dead,        Array.Empty<PlayerStateId>() },
        };

    public static bool Can(PlayerStateId from, PlayerStateId to) =>
        _allowed.TryGetValue(from, out var nexts) && Array.IndexOf(nexts, to) >= 0;

    /// <summary>
    /// Mapea un PlayerStateId a su PlayerActionId homónimo.
    /// Mantener explícito (switch) evita reflexiones y errores por renombre.
    /// </summary>
    public static PlayerActionId ToAction(PlayerStateId state) => state switch
    {
        PlayerStateId.Idle        => PlayerActionId.Idle,
        PlayerStateId.Walk        => PlayerActionId.Walk,
        PlayerStateId.Fall        => PlayerActionId.Fall,
        PlayerStateId.Shoot       => PlayerActionId.Shoot,
        PlayerStateId.Smash       => PlayerActionId.Smash,
        PlayerStateId.DropBandage => PlayerActionId.DropBandage,
        PlayerStateId.Push        => PlayerActionId.Push,
        PlayerStateId.Attract     => PlayerActionId.Attract,
        PlayerStateId.Swing       => PlayerActionId.Swing,
        PlayerStateId.Dead        => PlayerActionId.Dead,
        _ => PlayerActionId.Idle
    };
}