using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinState : State, IBandageRestrictor
{
    private PlayerContext _ctx;
    public WinState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.Animator.SetTrigger("Win");

        Debug.Log("////////////WINSTATE//////////");
        
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Win", true);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }
}