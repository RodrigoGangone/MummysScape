using UnityEngine;
using static PlayerEnum;

public sealed class AttractState : State
{
    private readonly PlayerContext _ctx;
    private BoxPushAttract _box;

    // Constantes
    private const float MIN_PULL_SPEED_FLOOR = 0.05f;
    private const float ROT_LERP = 1f;

    // Variable para controlar el delay
    private float _moveUnlockTime;

    public AttractState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("AttractState!");
        if (!_ctx.TryGetAttractTarget(out _box))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        // 1. Iniciamos la física y la animación (Wrap)
        _box.SetPushAttractMode(true, true);
        
        // 2. Calculamos cuándo termina la animación de vendado
        // Time.time actual + la duración que nos diga la caja
        _moveUnlockTime = Time.time + _box.GetAttachDuration();
    }

    public override void OnExit()
    {
        _box?.StopImmediate();
        _box?.SetPushAttractMode(false); // Esto dispara el UnWrap
        _box = null;
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        // --- Chequeos de validación (Salidas) ---
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerStateId.Fall);
            return;
        }
        // Permitimos cancelar incluso durante la animación de vendado
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

        // --- Lógica de Dirección y Rotación ---
        Vector3 playerPos = _ctx.Tf.position;
        Vector3 boxPos    = _box.transform.position;
        Vector3 toPlayer  = new Vector3(playerPos.x - boxPos.x, 0f, playerPos.z - boxPos.z);
        Vector3 dirPull   = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

        // Siempre rotamos al player hacia la caja, incluso mientras espera la animación
        Vector3 toBox = -dirPull;
        if (toBox.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(toBox, Vector3.up);
            var smooth    = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * ROT_LERP * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smooth);
        }

        // --- Lógica de Espera (Wait for Wrap) ---
        // Si el tiempo actual es menor al tiempo de desbloqueo, NO movemos la caja todavía.
        if (Time.time < _moveUnlockTime)
        {
            // Opcional: Podrías frenar la caja explícitamente si tuviera inercia residual
            _box.StopImmediate(); 
            return; 
        }

        // --- Lógica de Movimiento (Solo se ejecuta tras finalizar el Wrap) ---
        float min    = _ctx.AttractMinDistance;
        float max    = _ctx.AttractMaxDistance;
        float distXZ = _box.HorizontalDistanceTo(playerPos);

        if (distXZ <= min)
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        float t01 = Mathf.InverseLerp(min, max, Mathf.Clamp(distXZ, min, max));
        float curveMul = Mathf.Max(0f, _ctx.AttractSpeedCurve.Evaluate(t01));
        float pullSpeed = Mathf.Max(MIN_PULL_SPEED_FLOOR, _ctx.MoveSpeed * _ctx.AttractSpeedBase * curveMul);

        float maxStep = pullSpeed * Time.fixedDeltaTime;
        float allowed = Mathf.Max(0f, distXZ - min);
        float step    = Mathf.Min(maxStep, allowed);

        if (step > 0f) _box.MoveBy(dirPull * step);
    }
}