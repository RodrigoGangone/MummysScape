using UnityEngine;
using static PlayerEnum;

/// <summary>
/// PushState
/// Dueño de la interacción Player↔Caja mientras hay input de movimiento.
/// - Mueve player y caja con el mismo delta.
/// - Suelta si no hay suelo bajo la caja o se pierde el target.
/// - Rotación suave.
/// </summary>
public sealed class PushState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private BoxPushAttract _box;
    private float _halfSpeed;
    private const float INPUT_DEADZONE = 0.05f;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("PushState!");
        if (!_ctx.TryGetPushTarget(out _box, out _, out _))
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Walk);
            return;
        }

        _halfSpeed = _ctx.MoveSpeed * 0.5f;
    
        // CAMBIO AQUÍ: true = mover física, false = SIN vendas
        _box.SetPushAttractMode(true, false); 
        
        _ctx.View.Animator.SetBool("Push", true);
    }

    public override void OnExit()
    {
        _box?.StopImmediate();
        // Al salir, desactivamos física. El segundo parámetro da igual (por defecto true), 
        // porque al ser enabled=false, la lógica visual hará UnWrap de todas formas.
        _box?.SetPushAttractMode(false); 
        _box = null;
        
        _ctx.View.Animator.SetBool("Push", false);
    }

    public override void OnUpdate()
    {
        _halfSpeed = _ctx.MoveSpeed * 0.5f;
    }

    public override void OnFixedUpdate()
    {
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(PlayerStateId.Fall);
            return;
        }

        if (_box == null || !_ctx.TryGetPushTarget(out var stillBox, out _, out _) || stillBox != _box)
        {
            StateMachine.ChangeState(PlayerStateId.Walk);
            return;
        }

        if (!_box.IsGroundedForPushAttract())
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Walk);
            return;
        }

        Vector2 mv = _ctx.Input.Move;
        if (Mathf.Abs(mv.x) < INPUT_DEADZONE && Mathf.Abs(mv.y) < INPUT_DEADZONE)
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 delta = dir * (_halfSpeed * Time.fixedDeltaTime);
        _ctx.Rb.MovePosition(_ctx.Rb.position + delta);
        _box.MoveBy(delta);

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * Time.fixedDeltaTime);
        _ctx.Rb.MoveRotation(smoothRot);
    }
}