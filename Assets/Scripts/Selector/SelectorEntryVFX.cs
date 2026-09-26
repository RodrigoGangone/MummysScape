using UnityEngine;

public class SelectorEntryVfx : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField] private ParticleSystem[] particleSystems;

    [Header("Behaviour")]
    [Tooltip("Limpia cualquier emisión anterior antes de volver a reproducir el efecto.")]
    [SerializeField] private bool clearBeforePlay = true;

    public void Prepare()
    {
        if (particleSystems == null)
            return;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    public void Play()
    {
        if (particleSystems == null)
            return;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            if (clearBeforePlay)
            {
                ps.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }

            ps.Play(true);
        }
    }

    public void Stop()
    {
        if (particleSystems == null)
            return;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    private void OnDisable()
    {
        Stop();
    }
}