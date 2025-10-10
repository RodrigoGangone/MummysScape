using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimState : State
{
    private PlayerContext _ctx;
    public AimState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter() { Debug.Log("Aim") ;}
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}
