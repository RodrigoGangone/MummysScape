using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwingState : State
{
    private PlayerContext _ctx;

    public SwingState(PlayerContext ctx)
    {
        _ctx = ctx;
    }

    public override void OnEnter()
    {
        Debug.Log("Swing - OnEnter");
        
        if (_ctx.TryGetSwingTarget(out var hookRb))
        {
            _ctx.SwingHandler.SetSpring(true);

            _ctx.SwingHandler.SpringJoint.connectedBody = hookRb;
        }
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        Debug.Log("Swing - OnExit");
        _ctx.SwingHandler.SetSpring(false);
    }
}