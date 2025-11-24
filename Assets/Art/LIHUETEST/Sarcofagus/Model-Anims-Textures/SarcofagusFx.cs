using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SarcofagusFx : MonoBehaviour
{
    [SerializeField] private ParticleSystem fx1;
    [SerializeField] private ParticleSystem fx2;
    [SerializeField] private ParticleSystem fx3;
    [SerializeField] private ParticleSystem fx4;
    [SerializeField] private ParticleSystem fx5;
    [SerializeField] private ParticleSystem fx6;
    [SerializeField] private ParticleSystem fx7;

    public void PlayFx1()
    {
        fx1.Play();
    }

    public void PlayFx2()
    {
        fx2.Play();
    }

    public void PlayFx3()
    {
        fx3.Play();
    }
    public void PlayFx4()
    {
        fx4.Play();
    }
    
    public void PlayFx5()
    {
        fx5.Play();
    }
    
    public void PlayFx6()
    {
        fx6.Play();
    }
    
    public void PlayFx7()
    {
        fx7.Play();
    }
}
