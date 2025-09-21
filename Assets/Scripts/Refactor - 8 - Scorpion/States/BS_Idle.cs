using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

/// <summary> Estado Idle: sólo mantiene pose / mira al player si querés. </summary>
public sealed class BS_Idle : State
{
    private readonly BossActor _actor;
    public BS_Idle(BossActor actor) { _actor = actor; }
    public override void OnEnter()
    {
        Debug.Log("OnEnter Idle");
        _actor.Animator?.SetBool("Idle", true);
    }

    public override void OnFixedUpdate() { }
    public override void OnExit()  => _actor.Animator?.SetBool("Idle", false);
    public override void OnUpdate()
    {
        // Opcional: "mirar" al player sin inclinar
        var t = _actor.Transform;
        var p = _actor.Player.transform.position;
        p.y = t.position.y;
        t.LookAt(p);
    }
}