using System;
using static PlayerEnum;

/// <summary>
/// PlayerTransitionGuard
/// Valida la transición A->B con: matriz de transiciones + SizeRules.
/// No duplica lógica en los States. Abreviado y testeable.
/// </summary>
public sealed class PlayerTransitionGuard : IStateTransitionGuard
{
    private readonly PlayerContext _ctx;

    public PlayerTransitionGuard(PlayerContext ctx) => _ctx = ctx;

    public bool Can(Enum from, Enum to)
    {
        // Soporte inicial (sin estado previo): permitir el primer estado.
        if (from is null) return true;

        // Tipado fuerte esperado
        if (from is not PlayerStateId f || to is not PlayerStateId t) return false;

        // 1) Matriz de adyacencia
        if (!TransitionRules.Can(f, t)) return false;

        // 2) Reglas por tamaño (acción del estado destino)
        var action = TransitionRules.ToAction(t);
        if (!SizeRules.Can(_ctx.Model.Size, action)) return false;  // usa tu SizeRules actual :contentReference[oaicite:0]{index=0}

        return true;
    }
}

