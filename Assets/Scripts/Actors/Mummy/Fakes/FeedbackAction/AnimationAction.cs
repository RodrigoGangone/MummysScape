using UnityEngine;

// Ejemplo para Animaciones (Usando la Actions Layer que hablamos)
[CreateAssetMenu(menuName = "Fakes/Feedback/Animation")]
public class AnimationAction : FeedbackAction
{
    public string triggerName;
    public override void Play(PlayerContext ctx) => ctx.View.Animator.SetTrigger(triggerName);
}