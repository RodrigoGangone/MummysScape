using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeMummyVFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem winParticle;
    [SerializeField] private ParticleSystem loseParticle;
    [SerializeField] private ParticleSystem dustParticle;
    [SerializeField] private ParticleSystem shiningParticle;
    [SerializeField] private ParticleSystem auraParticle;
    
    public void OnCelebrate()
    {
        winParticle.Play();
        shiningParticle.Play();
    }

    public void OnLose()
    {
        loseParticle.Play();
    }

    public void OnFall()
    {
        dustParticle.Play();
    }

    public void PlayAura()
    {
        auraParticle.Play();
    }
}
