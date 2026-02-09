using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadState : State, IBandageRestrictor
{
    private PlayerContext _ctx;
    public DeadState(PlayerContext ctx) => _ctx = ctx;
    
    public override void OnEnter() 
    {
        _ctx.View.Animator.SetTrigger("Death");
        //GameEventManager.Instance.levelEvents.OnDeath.Raise();
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Death", true);
    }
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}