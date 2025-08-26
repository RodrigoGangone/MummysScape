using UnityEngine;
using static PlayerEnum;

/// <summary>
/// SmashState
/// Ejecuta el smash (placeholder) y vuelve a Idle. Solo permitido en Head.
/// </summary>
public sealed class SmashState : State
{
    private readonly PlayerContext _ctx;
    public SmashState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        if (!SizeRules.Can(_ctx.Model.Size, PlayerActionId.Smash))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        // TODO: tu lógica real (AOE/daño al suelo/trigger de puzzle)
        Debug.Log("Smash!");
        _ctx.View?.PlaySmash();

        StateMachine.ChangeState(PlayerStateId.Idle);
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}