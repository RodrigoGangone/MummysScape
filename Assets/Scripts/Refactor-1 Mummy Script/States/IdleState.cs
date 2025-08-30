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

    public override void OnEnter() { _ctx.View?.SetMoveSpeedVisual(0f); Debug.Log("IdleState!"); }
    public override void OnExit()  { }

    public override void OnUpdate()
    {
        //TODO: todavia no hay un fallstate
        //if (!_ctx.IsGrounded()) { StateMachine.ChangeState(PlayerStateId.Fall); return; }

        var mv = _ctx.Input.Move;
        if (Mathf.Abs(mv.x) > 0.01f || Mathf.Abs(mv.y) > 0.01f)
        {
            //TODO: todavia no hay un pushstate
            // ¿hay caja adelante? -> Push, si está permitido por Size
            //if (_ctx.HasPushableAhead() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.Push))
            //    StateMachine.ChangeState(PlayerStateId.Push);
            //else
            StateMachine.ChangeState(PlayerStateId.Walk);
            return;
        }

        // Q -> DropBandage 
        if (_ctx.Input.ConsumeDropDown())
        {
            StateMachine.ChangeState(PlayerStateId.DropBandage);
            return;
        } 
        
        // E -> Shoot
        if (_ctx.Input.ConsumeShootDown())
        {
            StateMachine.ChangeState(PlayerStateId.Shoot);
            return;
        }

        // Space -> primero Smash (si estás en Head), sino Attract (si target al frente)
        if (_ctx.Input.ConsumeSmashDown())
        {
            StateMachine.ChangeState(PlayerStateId.Smash);
            return;
        }
        
        if (_ctx.Input.ConsumeAttractDown())
        {
            // opcional: verificá target antes de entrar
            if (_ctx.TryGetAttractTarget(out _))
            {
                StateMachine.ChangeState(PlayerStateId.Attract);
                return;
            }
        }
    }

    public override void OnFixedUpdate() { }
}