using UnityEngine;
using static PlayerEnum;

/// <summary>
/// AttractState
/// Atrae la caja hacia el player en XZ respetando SIEMPRE la distancia mínima:
/// - Sale cuando la distancia horizontal a la superficie de la caja <= min.
/// - Clampea el paso para no “pasarse”.
/// - El player rota suavemente mirando al centro horizontal de la caja.
/// </summary>
public sealed class AttractState : State
{
    private readonly PlayerContext _ctx;
    private BoxPushAttract _box;

    private float _pullSpeed;
    private const float ROT_LERP = 1f;

    public AttractState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("AttractState!");
        if (!_ctx.TryGetAttractTarget(out _box))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        _pullSpeed = Mathf.Max(0.1f, _ctx.MoveSpeed * 0.8f);
        _box.SetPushAttractMode(true);
    }

    public override void OnExit()
    {
        _box?.StopImmediate();
        _box?.SetPushAttractMode(false);
        _box = null;
    }

    public override void OnUpdate()
    {
        _pullSpeed = Mathf.Max(0.1f, _ctx.MoveSpeed * 0.8f);
    }

    public override void OnFixedUpdate()
    {
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerStateId.Fall);
            return;
        }
        if (!_ctx.Input.IsSpaceHeld())
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }
        if (_box == null || !_box.IsGroundedForPushAttract())
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        Vector3 playerPos = _ctx.Tf.position;
        Vector3 boxPos    = _box.transform.position;

        // Distancia HORIZONTAL real a la superficie del collider de la caja
        float min = _ctx.AttractMinDistance;
        float horizDist = _box.HorizontalDistanceTo(playerPos);

        // ✅ Salir si ya estamos en / por debajo del mínimo
        if (horizDist <= min)
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        // Dirección de atracción (centro caja -> player) en XZ
        Vector3 toPlayer = new(playerPos.x - boxPos.x, 0f, playerPos.z - boxPos.z);
        Vector3 dirPull = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

        // Paso máximo por frame
        float maxStep = _pullSpeed * Time.fixedDeltaTime;

        // ✅ Clampeo: nunca mover más de (distancia_actual - min)
        float allowed = Mathf.Max(0f, horizDist - min);
        float step = Mathf.Min(maxStep, allowed);

        if (step > 0f) _box.MoveBy(dirPull * step);

        // Rotación suave mirando hacia el centro horizontal de la caja
        Vector3 toBox = new(boxPos.x - playerPos.x, 0f, boxPos.z - playerPos.z);
        if (toBox.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toBox.normalized, Vector3.up);
            Quaternion smooth = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * ROT_LERP * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smooth);
        }
    }
}