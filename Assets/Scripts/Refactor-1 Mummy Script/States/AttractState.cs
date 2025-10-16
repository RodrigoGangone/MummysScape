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

    // Piso de velocidad para evitar “quedarse corto” si la curva vale ~0 lejos.
    private const float MIN_PULL_SPEED_FLOOR = 0.05f;
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

        // Distancia horizontal REAL a la superficie de la caja
        float min    = _ctx.AttractMinDistance;
        float max    = _ctx.AttractMaxDistance;
        float distXZ = _box.HorizontalDistanceTo(playerPos);

        // Salida si ya llegó al mínimo
        if (distXZ <= min)
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Idle);
            return;
        }

        // Normalización 0..1 (0 en min, 1 en max)
        float t01 = Mathf.InverseLerp(min, max, Mathf.Clamp(distXZ, min, max));

        // Velocidad por curva * base * MoveSpeed del size actual
        float curveMul = Mathf.Max(0f, _ctx.AttractSpeedCurve.Evaluate(t01));
        float pullSpeed = Mathf.Max(MIN_PULL_SPEED_FLOOR, _ctx.MoveSpeed * _ctx.AttractSpeedBase * curveMul);

        // Dirección caja -> player (plano XZ) + clamp para no "pasarse" del mínimo
        Vector3 toPlayer = new Vector3(playerPos.x - boxPos.x, 0f, playerPos.z - boxPos.z);
        Vector3 dirPull  = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

        float maxStep = pullSpeed * Time.fixedDeltaTime;
        float allowed = Mathf.Max(0f, distXZ - min);
        float step    = Mathf.Min(maxStep, allowed);

        if (step > 0f) _box.MoveBy(dirPull * step);

        // Rotación suave del player mirando al centro de la caja (horizontal)
        Vector3 toBox = -dirPull; // player -> caja
        if (toBox.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(toBox, Vector3.up);
            var smooth    = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * ROT_LERP * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smooth);
        }
    }
}