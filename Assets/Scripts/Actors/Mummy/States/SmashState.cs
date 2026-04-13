using UnityEngine;
using static Animations.Player;

/// <summary>
/// SmashState
/// Ejecuta el smash (placeholder) y vuelve a Idle. Solo permitido en Head.
/// </summary>
public sealed class SmashState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    public SmashState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool(SMASH, true);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _ctx.View.Animator.SetBool(SMASH, false);
    }
}