using UnityEngine;
using static PlayerEnum;
using static Animations.Player;
using static SfxIDs;

/// <summary> 
/// Estado de Empuje: Sincroniza el movimiento del jugador con una caja interactuable, permitiendo 
/// el desplazamiento conjunto siempre que se mantenga el contacto físico y el suelo bajo ambos. 
/// </summary>

public sealed class PushState : State, IBandageRestrictor, IFailableState
{
    private readonly PlayerContext _ctx;
    private BoxPushAttract _box;
    private float _halfSpeed;
    private const float INPUT_DEADZONE = 0.05f;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.PlaySfx(Mummy___Normal.WalkPush);
        
        if (!_ctx.TryGetPushTarget(out _box, out _, out _))
        {
            StateMachine.ChangeState(PlayerStateId.Walk);
            return;
        }

        _halfSpeed = _ctx.MoveSpeed * 0.5f;
    
        _box.SetPushAttractMode(true, false); 
        _box.bank.Play3D(Box.MoveBox, _box.transform.position);
        _ctx.View.Animator.SetBool(PUSH, true);
    }

    public override void OnExit()
    {
        _box?.StopImmediate();
        _box.bank.Stop(Box.MoveBox);
        _box?.SetPushAttractMode(false); 
        _box = null;
        
        _ctx.View.StopSfx(Mummy___Normal.WalkPush);

        _ctx.View.Animator.SetBool(PUSH, false);
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
            StateMachine.ChangeState(PlayerStateId.Walk);
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

    public void OnTransitionDenied(PlayerSize currentSize)
    {
        _ctx.View.Animator.SetBool("FakePush", true);
        
        _ctx.View.HandleFailedTransition(PlayerStateId.Push,
            currentSize,
            _ctx);
    }
}