using UnityEngine;
using static PlayerEnum;

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
        Debug.Log("Smash!");
        _ctx.View.Animator.SetBool("Smash", true);
        //GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Smash", true);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _ctx.View.PlaySfx("SmashExit");
       // GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Smash", false);
        _ctx.View.Animator.SetBool("Smash", false);
    }
}