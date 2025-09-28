using System.Collections;
using UnityEngine;
using static Utils;

/// <summary> Estado que ejecuta Skill B y vuelve a Idle. </summary>
public sealed class BS_UseSkillB : State
{
    private readonly BossActor _actor;
    private bool _fired;
    public BS_UseSkillB(BossActor actor) { _actor = actor; }

    public override void OnEnter()
    {
        _actor.NotifySkillStarted();

        _fired = _actor.TryUseSkillB();
        
        _actor.Animator.SetBool(SECONDARY_ANIM_SCORPION, true);
        
        if (!_fired)
            _actor.NotifySkillEnded();
        else
            _actor.StartCoroutine(ReturnAfter(1f));
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() => _actor.Animator.SetBool(SECONDARY_ANIM_SCORPION, false);
    private IEnumerator ReturnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        _actor.NotifySkillEnded();
    }
}