using UnityEngine;
using static PlayerEnum;

//// <summary>
//// PushState
//// Dueño de la interacción Player↔Caja:
////  - Mantiene el estado hasta: (a) input de movimiento ~0, (b) pierde TryGetPushTarget, (c) cae (no grounded).
////  - Mueve al player y a la caja con el MISMO delta (sin aceleración).
////  - Rotación suave a la dirección.
////  - Opcional: ignora colisiones player↔caja mientras dura el push.
//// </summary>
public sealed class PushState : State
{
    private readonly PlayerContext _ctx;
    private BoxPushAttract _box;
    private float _halfSpeed;
    private const float INPUT_DEADZONE = 0.05f;

    // Toggle opcional: si true, evita por completo reacción física entre ambos.
    private const bool IGNORE_COLLISIONS_DURING_PUSH = false;
    private System.Collections.Generic.List<(Collider a, Collider b)> _ignoredPairs;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        if (!_ctx.TryGetPushTarget(out _box, out _, out _))
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Walk);
            return;
        }

        _halfSpeed = _ctx.MoveSpeed * 0.5f;

        if (IGNORE_COLLISIONS_DURING_PUSH)
        {
            _ignoredPairs = new();
            var playerCols = _ctx.Tf.GetComponentsInChildren<Collider>();
            var boxCols    = _box.GetComponentsInChildren<Collider>();
            foreach (var pc in playerCols)
                foreach (var bc in boxCols)
                {
                    if (!pc || !bc) continue;
                    Physics.IgnoreCollision(pc, bc, true);
                    _ignoredPairs.Add((pc, bc));
                }
        }
        _box.SetPushMode(true);
        Debug.Log("PushState!");
    }

    public override void OnExit()
    {
        if (IGNORE_COLLISIONS_DURING_PUSH && _ignoredPairs != null)
        {
            foreach (var (a,b) in _ignoredPairs)
                if (a && b) Physics.IgnoreCollision(a, b, false);
            _ignoredPairs.Clear();
            _ignoredPairs = null;
        }

        _box?.StopImmediate();
        _box?.SetPushMode(false); // ⬅️ vuelve a congelar XZ fuera de push
        _box = null;
    }

    public override void OnUpdate()
    {
        // Refresca por si cambia Size.
        _halfSpeed = _ctx.MoveSpeed * 0.5f;
    }

    public override void OnFixedUpdate()
    {
        // 0) Si caigo, salgo a Fall
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Fall);
            return;
        }

        // 1) Si pierdo la caja o cambia la referencia, salgo a Walk
        if (_box == null || !_ctx.TryGetPushTarget(out var stillBox, out _, out _) || stillBox != _box)
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Walk);
            return;
        }

        // 2) Sin input => Idle
        Vector2 mv = _ctx.Input.Move;
        if (Mathf.Abs(mv.x) < INPUT_DEADZONE && Mathf.Abs(mv.y) < INPUT_DEADZONE)
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Idle);
            return;
        }

        // 3) Mover ambos con el MISMO delta
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 delta = dir * (_halfSpeed * Time.fixedDeltaTime);

        // Player
        _ctx.Rb.MovePosition(_ctx.Rb.position + delta);

        // Caja
        _box.MoveBy(delta);

        // Rotación suave
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * Time.fixedDeltaTime);
        _ctx.Rb.MoveRotation(smoothRot);
    }
}