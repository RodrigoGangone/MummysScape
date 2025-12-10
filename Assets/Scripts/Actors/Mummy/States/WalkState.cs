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

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool("Walk", true);
        Debug.Log("WalkState!");
    }

    public override void OnExit()
    {
        _ctx.View.Animator.SetBool("Walk", false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        Vector2 mv = _ctx.Input.Move;
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);

        if (!(dir.sqrMagnitude > 0.0001f)) return;

        Vector3 targetPos = _ctx.Rb.position + dir * (_ctx.MoveSpeed * Time.fixedDeltaTime);
        _ctx.Rb.MovePosition(targetPos);

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * Time.fixedDeltaTime);
        _ctx.Rb.MoveRotation(smoothRot);

        _ctx.View?.SetMoveSpeedVisual(1f);
    }
}