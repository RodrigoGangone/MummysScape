using System;
using UnityEngine;
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
    
    /// <summary>
    /// CanEnterPush
    /// Regla dinámica de entrada a Push:
    /// - Requiere input de movimiento por encima de la deadzone.
    /// - Requiere un target de empuje válido (cara/eje).
    /// - Alineación mínima al eje permitido (DOT).
    /// - Mirar ~hacia el centro horizontal de la caja (ángulo máximo).
    /// Usa _ctx para leer input, cámara y geometría actual.
    /// </summary>
    public bool CanEnterPush(float moveDeadZone, float minAxisDot, float maxAngleDeg)
    {
        var mv = _ctx.Input.Move;
        if (Mathf.Abs(mv.x) <= moveDeadZone && Mathf.Abs(mv.y) <= moveDeadZone)
            return false;

        if (!_ctx.TryGetPushTarget(out var box, out var face))
            return false;

        Vector3 desiredDir = _ctx.CameraRelativeDir(mv.x, mv.y);
        Vector3 axis = box.GetPushAxis(face);
        float axisDot = Vector3.Dot(desiredDir, axis);
        if (axisDot < minAxisDot)
            return false;

        Vector3 playerPos = _ctx.Rb.position;
        Vector3 toBox = box.transform.position - playerPos; 
        toBox.y = 0f;

        if (toBox.sqrMagnitude > 1e-6f)
        {
            float angle = Vector3.Angle(_ctx.Rb.rotation * Vector3.forward, toBox.normalized);
            if (angle > maxAngleDeg)
                return false;
        }

        return true;
    }
}

