using System;
using static PlayerEnum;

public sealed class PlayerTransitionGuard : IStateTransitionGuard
{
    private readonly PlayerContext _ctx;

    public PlayerTransitionGuard(PlayerContext ctx) => _ctx = ctx;

    public bool Can(Enum from, Enum to)
    {
        if (to is not PlayerStateId t) return false;
        if (from is null) return SizeRules.Can(_ctx.Model.Size, t);
        if (from is not PlayerStateId f) return false;

        // 1. Validación de matriz de transiciones estructurales
        if (!TransitionRules.Can(f, t)) return false;

        // 2. Red de seguridad: si el Driver dejó pasar algo o se fuerza un cambio, el Guard deniega.
        if (!SizeRules.Can(_ctx.Model.Size, t)) return false;
        
        return true;
    }
}