using UnityEngine;
using static PlayerEnum;

public sealed class ShootState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;

    public ShootState(PlayerContext ctx)
    {
        _ctx = ctx;
    }

    public override void OnEnter()
    {
        _ctx.View.PlaySfx("Shoot");
        
        _ctx.View.Animator.SetBool("Shoot", true);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _ctx.View.Animator.SetBool("Shoot", false);
    }
}
