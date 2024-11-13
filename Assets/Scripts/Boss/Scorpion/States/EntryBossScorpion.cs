using System.Collections;
using UnityEngine;
using static Utils;

public class EntryBossScorpion : State
{
    private Scorpion _scorpion;

    public EntryBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }
    
    public override void OnEnter()
    {
        _scorpion.StartCoroutine(WaitForAnimationEnd(_scorpion._anim, ENTRY_NAME_ANIM_SCORPION));
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }

    private IEnumerator WaitForAnimationEnd(Animator scorpionAnimator, string animationName)
    {
        yield return null;

        AnimatorStateInfo currentStateInfo = scorpionAnimator.GetCurrentAnimatorStateInfo(0);

        while (currentStateInfo.IsName(animationName) && currentStateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            currentStateInfo = scorpionAnimator.GetCurrentAnimatorStateInfo(0);
        }

        _scorpion.stateMachine.ChangeState(BossScorpionState.IdleScorpion);
    }
}
