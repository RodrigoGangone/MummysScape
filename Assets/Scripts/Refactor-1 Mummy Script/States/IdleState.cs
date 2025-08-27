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

        { StateMachine.ChangeState(PlayerStateId.Walk); return; }

        // Q -> DropBandage (si permitido)
        if (_ctx.Input.ConsumeDropDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.DropBandage))
        { StateMachine.ChangeState(PlayerStateId.Shoot /*placeholder*/); } // <- reemplazar por PlayerStateId.Drop si lo definiste
        // ^^^ ver nota debajo

        // Space -> primero Smash (si estás en Head), sino Attract (si target al frente)
        if (SizeRules.Can(_ctx.Model.Size, PlayerActionId.Smash) && _ctx.Input.ConsumeSmashDown())
        { StateMachine.ChangeState(PlayerStateId.Smash); return; }

        if (SizeRules.Can(_ctx.Model.Size, PlayerActionId.Attract) && _ctx.Input.ConsumeAttractDown())
        {
            // opcional: verificá target antes de entrar
            if (_ctx.TryGetAttractTarget(out _)) { StateMachine.ChangeState(PlayerStateId.Attract /*placeholder*/); } // <- reemplazar por PlayerStateId.Attract
        }

        // E -> Shoot
        if (_ctx.Input.ConsumeShootDown() && SizeRules.Can(_ctx.Model.Size, PlayerActionId.Shoot))
        { StateMachine.ChangeState(PlayerStateId.Shoot); }
    }

    public override void OnFixedUpdate() { }
}