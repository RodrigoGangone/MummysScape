using System;
using UnityEngine;

/// <summary>
/// PushState
/// Mantiene el ciclo de empuje: proyecta el input al eje permitido, mueve la caja vía IPushable,
/// hace soft‑snap lateral del player al centro horizontal de la cara y lo alinea mirando a la caja.
/// No gestiona las transiciones (eso lo hace el Driver); asume que al entrar hay un target válido.
/// </summary>
public sealed class PushState : State
{
    private const float RotationLerpSpeed = 12f;

    private readonly PlayerContext _ctx;

    private PushInfo _info;

    public PushState(PlayerContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
    }

    public void SetPushInfo(PushInfo info) => _info = info;

    public override void OnEnter()
    {
        AlignRotation(0f);
    }

    public override void OnExit()
    {
        _info = default;
    }

    public override void OnUpdate()
    {
        AlignRotation(Time.deltaTime);
    }

    public override void OnFixedUpdate()
    {
    }

    private void AlignRotation(float deltaTime)
    {
        if (_ctx?.Tf == null || _info.Pushable == null)
        {
            return;
        }

        Vector3 direction = _info.GetHorizontalDirectionFrom(_ctx.Tf.position);
        if (direction == Vector3.zero)
        {
            direction = -_info.FaceNormal;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            direction.Normalize();
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        float lerpFactor = deltaTime > 0f ? Mathf.Clamp01(deltaTime * RotationLerpSpeed) : 1f;
        _ctx.Tf.rotation = Quaternion.Slerp(_ctx.Tf.rotation, targetRotation, lerpFactor);
    }
}
