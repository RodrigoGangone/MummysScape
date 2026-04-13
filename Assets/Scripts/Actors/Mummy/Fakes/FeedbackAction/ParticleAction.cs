using UnityEngine;

// Ejemplo para Partículas
[CreateAssetMenu(menuName = "Fakes/Feedback/Particle")]
public class ParticleAction : FeedbackAction
{
    public GameObject particlePrefab;
    public bool attachToPlayer = true;

    public override void Play(PlayerContext ctx)
    {
        var pos = ctx.Tf.position;
        var rot = ctx.Tf.rotation;
        var obj = Instantiate(particlePrefab, pos, rot);
        if (attachToPlayer) obj.transform.SetParent(ctx.Tf);
    }
}