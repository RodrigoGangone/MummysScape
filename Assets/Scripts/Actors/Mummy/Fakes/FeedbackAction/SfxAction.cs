using UnityEngine;

// Ejemplo para SFX (Usando el FxBank de tu View)
[CreateAssetMenu(menuName = "Fakes/Feedback/SFX")]
public class SfxAction : FeedbackAction
{
    public string sfxKey;
    public override void Play(PlayerContext ctx) => ctx.View.PlaySfx(sfxKey);
}