using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinState : State, IBandageRestrictor
{
    private PlayerContext _ctx;
    public WinState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        GameEventManager.Instance.levelEvents.OnWin.Raise();
    }
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}
