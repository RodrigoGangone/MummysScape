using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Estado de muerte. </summary>
public sealed class BS_Die : State
{
    private readonly BossActor _actor;
    public BS_Die(BossActor actor) { _actor = actor; }
    public override void OnEnter() => _actor.Animator?.SetTrigger("Die");
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}