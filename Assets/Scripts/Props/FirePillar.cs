using UnityEngine;

/// <summary> 
/// Emisor de Peligro: Script de control simple para activar o desactivar sistemas de partículas 
/// de fuego, optimizando la emisión y limpieza de partículas en tiempo real. 
/// </summary>
public class FirePillar : MonoBehaviour
{
    [Header("Settings")] [SerializeField] private bool playOnAwake = true;

    [Header("References")] [SerializeField]
    private ParticleSystem fireParticle;

    private void Awake()
    {
        if (fireParticle == null)
            fireParticle = GetComponentInChildren<ParticleSystem>();

        if (playOnAwake && fireParticle != null)
            fireParticle.Play();
    }

    public void SetState(bool isActive)
    {
        if (fireParticle == null) return;

        if (isActive)
        {
            if (!fireParticle.isPlaying) fireParticle.Play();
        }
        else
        {
            if (fireParticle.isPlaying) fireParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}