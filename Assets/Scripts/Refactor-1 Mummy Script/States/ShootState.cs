using UnityEngine;
using static PlayerEnum;

/// <summary>
/// ShootState
/// Ejecuta el disparo (placeholder) y vuelve a Idle. Valida SizeRules.
/// </summary>
public sealed class ShootState : State
{
    private readonly PlayerContext _ctx;
    public ShootState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        if (!SizeRules.Can(_ctx.Model.Size, PlayerActionId.Shoot))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        // TODO: tu raycast/proyectil real
        Debug.Log("Shoot!");
        _ctx.View?.PlayShoot();

        StateMachine.ChangeState(PlayerStateId.Idle);
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}