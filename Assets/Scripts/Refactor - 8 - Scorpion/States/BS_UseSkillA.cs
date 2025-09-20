using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Estado que ejecuta Skill A y vuelve a Idle. </summary>
public sealed class BS_UseSkillA : State
{
    private readonly BossActor _actor;
    private bool _fired;
    public BS_UseSkillA(BossActor actor) { _actor = actor; }

    public override void OnEnter()
    {
        _fired = _actor.TryUseSkillA();
        // Si la skill dispara una animación, dejá que la anim llame un evento y desde allí vuelvas a Idle.
        // Para simplificar, si no hay anim, volvemos pronto:
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
