using UnityEngine;
using static PlayerEnum;
/// <summary>
/// WalkState
/// Movimiento con Rigidbody relativo a la cámara. Rotación suave hacia dirección de marcha.
/// </summary>
public sealed class WalkState : State
{
    private readonly PlayerContext _ctx;
    public WalkState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter() { }
    public override void OnExit()  { }

    public override void OnUpdate()
    {
        var mv = _ctx.Input.Move;
        if (Mathf.Abs(mv.x) < 0.01f && Mathf.Abs(mv.y) < 0.01f)
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        if (_ctx.Input.ConsumeShootDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.Shoot))
            StateMachine.ChangeState(PlayerStateId.Shoot);

        if (_ctx.Input.ConsumeSmashDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.Smash))
            StateMachine.ChangeState(PlayerStateId.Smash);
    }

    public override void OnFixedUpdate()
    {
        Vector2 mv = _ctx.Input.Move;
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);

        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector3 targetPos = _ctx.Rb.position + dir * (_ctx.MoveSpeed * Time.fixedDeltaTime);
            _ctx.Rb.MovePosition(targetPos);

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smoothRot);

            _ctx.View?.SetMoveSpeedVisual(1f);
        }
        else
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
        }
    }
}