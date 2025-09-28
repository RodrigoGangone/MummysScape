using System.Collections;
using UnityEngine;
using static Utils;
/// <summary> Estado que ejecuta Skill A y vuelve a Idle. </summary>
public sealed class BS_UseSkillA : State
{
    private readonly BossActor _actor;
    private bool _fired;
    public BS_UseSkillA(BossActor actor) { _actor = actor; }

    public override void OnEnter()
    {
        _actor.NotifySkillStarted();

        _fired = _actor.TryUseSkillA();
        
        _actor.Animator.SetBool(PRIMARY_ANIM_SCORPION, true);
        
        if (!_fired)
            _actor.NotifySkillEnded();
        else
            _actor.StartCoroutine(ReturnAfter(2f));
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() => _actor.Animator.SetBool(PRIMARY_ANIM_SCORPION, false);
    private IEnumerator ReturnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        _actor.NotifySkillEnded();
    }
}
