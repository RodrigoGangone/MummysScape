using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Estado que ejecuta Skill B y vuelve a Idle. </summary>
public sealed class BS_UseSkillB : State
{
    private readonly BossActor _actor;
    private bool _fired;
    public BS_UseSkillB(BossActor actor) { _actor = actor; }

    public override void OnEnter()
    {
        _fired = _actor.TryUseSkillB();
        if (!_fired)
            _actor.TriggerFSM("Idle");
        else
            _actor.StartCoroutine(ReturnAfter(0.2f));
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
    private IEnumerator ReturnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        _actor.TriggerFSM("Idle");
    }
}