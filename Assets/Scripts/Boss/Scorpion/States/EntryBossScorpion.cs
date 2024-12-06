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
        _scorpion.Effects.entryScorpion.Play();

        _scorpion.StartCoroutine(WaitForAnimationEnd(_scorpion.anim, ENTRY_NAME_ANIM_SCORPION,
            _scorpion.Effects.entryScorpion));
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

    private IEnumerator WaitForAnimationEnd(Animator scorpionAnimator, string animationName, ParticleSystem entryEffect)
    {
        while (entryEffect.isPlaying)
            yield return null;

        _scorpion.anim.SetTrigger(ENTRY_ANIM_SCORPION);

        AnimatorStateInfo currentStateInfo = scorpionAnimator.GetCurrentAnimatorStateInfo(0);

        while (!currentStateInfo.IsName(animationName))
        {
            yield return null;
            currentStateInfo = scorpionAnimator.GetCurrentAnimatorStateInfo(0);
        }

        while (currentStateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            currentStateInfo = scorpionAnimator.GetCurrentAnimatorStateInfo(0);
        }

        _scorpion.stateMachine.ChangeState(ScorpionState.IdleScorpion);
    }
}