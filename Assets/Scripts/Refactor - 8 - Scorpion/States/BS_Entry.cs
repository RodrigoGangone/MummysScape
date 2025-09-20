using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Estado de entrada (intro). </summary>
public sealed class BS_Entry : State
{
    private readonly BossActor _actor;
    public BS_Entry(BossActor actor) { _actor = actor; }
    public override void OnEnter()
    {
        _actor.Animator?.SetTrigger("Entry");
        // Si no hay anim, podrías saltar directo a Idle
        _actor.TriggerFSM("Idle");
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}