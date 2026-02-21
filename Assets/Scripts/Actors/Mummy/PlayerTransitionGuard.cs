using System;
using static PlayerEnum;

/// <summary> 
/// Validador de Transiciones: Cruza los datos del modelo con las matrices de reglas (Transition & Size Rules) 
/// para autorizar o denegar el paso de un estado a otro en la FSM. 
/// </summary>

public sealed class PlayerTransitionGuard : IStateTransitionGuard
{
    private readonly PlayerContext _ctx;

    public PlayerTransitionGuard(PlayerContext ctx) => _ctx = ctx;
    
    public bool Can(Enum from, Enum to)
    {
        // si a donde va no es un PlayerStateID -> false
        if (to is not PlayerStateId t) return false;

        // si no hay estado previo aún, permitimos el primero (ej: Idle inicial)
        if (from is null) return SizeRules.Can(_ctx.Model.Size, t);
        
        // si de donde viene  no es un PlayerStateID -> false
        if (from is not PlayerStateId f) return false;

        // 1) si no puede transicionar de donde viene a donde va -> false
        if (!TransitionRules.Can(f, t)) return false;

        // 2) reglas por tamaño
        if (!SizeRules.Can(_ctx.Model.Size, t)) return false;

        return true;
    }
}

