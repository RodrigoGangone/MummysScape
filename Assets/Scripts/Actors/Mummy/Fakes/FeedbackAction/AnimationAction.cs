using UnityEngine;

// Ejemplo para Animaciones (Usando la Actions Layer que hablamos)
[CreateAssetMenu(menuName = "Fakes/Feedback/Animation")]
public class AnimationAction : FeedbackAction
{
    public string triggerName;
    public override void Play(PlayerContext ctx)
    {
        if (ctx?.View?.Animator == null || string.IsNullOrWhiteSpace(triggerName)) return;

        Animator animator = ctx.View.Animator;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }

        Debug.LogWarning($"AnimationAction trigger '{triggerName}' was not found on the current Animator.");
    }
}
