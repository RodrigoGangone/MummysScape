using UnityEngine;
using static PlayerEnum;
/// <summary>
/// IdleState
/// Espera input de movimiento para pasar a Walk; escucha acciones para saltar a otros estados.
/// </summary>
public sealed class IdleState : State
{
    private readonly PlayerContext _ctx;
    public IdleState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter() { _ctx.View?.SetMoveSpeedVisual(0f); }
    public override void OnExit()  { }

    public override void OnUpdate()
    {
        var mv = _ctx.Input.Move;
        if (Mathf.Abs(mv.x) > 0.01f || Mathf.Abs(mv.y) > 0.01f)
        {
            StateMachine.ChangeState(PlayerStateId.Walk);
            return;
        }

        if (_ctx.Input.ConsumeShootDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.Shoot))
            StateMachine.ChangeState(PlayerStateId.Shoot);

        if (_ctx.Input.ConsumeSmashDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.Smash))
            StateMachine.ChangeState(PlayerStateId.Smash);
    }

    public override void OnFixedUpdate() { }
}