using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

/// <summary> Estado de muerte. </summary>
public sealed class BS_Die : State
{
    private readonly BossActor _actor;
    public BS_Die(BossActor actor) { _actor = actor; }
    public override void OnEnter() => _actor.Animator?.SetTrigger(DIE_ANIM_SCORPION);
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}