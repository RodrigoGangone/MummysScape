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
        if (to is not PlayerStateId t) return false;
        if (from is null) return SizeRules.Can(_ctx.Model.Size, t);
        if (from is not PlayerStateId f) return false;

        if (!TransitionRules.Can(f, t)) return false;

        // REGLAS POR TAMAÑO
        if (!SizeRules.Can(_ctx.Model.Size, t))
        {
            // Recuperamos el estado al que intentamos ir
            var targetState = _ctx.StateMachine.GetState(t);
        
            // Si el estado tiene implementada la interfaz de fallo, la ejecutamos
            if (targetState is IFailableState failable)
            {
                failable.OnTransitionDenied(_ctx.Model.Size);
            }
            return false;
        }

        return true;
    }
}

