using System.Collections;
using UnityEngine;
using static Animations.Boss;
    
/// <summary>
/// Estado de Daño: Ejecuta la animación de impacto y suspende la toma de decisiones hasta que 
/// la animación finaliza, notificando entonces la recuperación del Boss.
/// </summary>

public class BS_Angry : State
{
    private readonly BossActor _actor;
    public BS_Angry(BossActor actor) { _actor = actor; }

    public override void OnEnter()
    {
        _actor.Animator.SetTrigger(ANGRY_ANIM_SCORPION);
        
        _actor.StartCoroutine(WaitForAnimationEnd(_actor.Animator, ANGRY_ANIM_SCORPION));
    }
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }

    public override void OnExit() { }
    
    private IEnumerator WaitForAnimationEnd(Animator scorpionAnimator, string animationName)
    {
        yield return null;

        AnimatorStateInfo currentStateInfo = scorpionAnimator.GetCurrentAnimatorStateInfo(0);

        while (currentStateInfo.IsName(animationName) && currentStateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            currentStateInfo = scorpionAnimator.GetCurrentAnimatorStateInfo(0);
        }
        
        _actor.NotifyRecovery();
    }
}
