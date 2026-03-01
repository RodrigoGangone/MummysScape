using System;
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
    [SerializeField] private ParticleSystem fx8;

    [SerializeField] private FxBank bank;

    [SerializeField] private string keySound01;
    [SerializeField] private string keySound02;
    [SerializeField] private string keySound03;
    [SerializeField] private string keySound04;
    [SerializeField] private string keySound05;

    private const string KEY_BOUNCE_SOUND = "Bounce";
    
    private Portal Portal => GetComponentInParent<Portal>();
    
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
        bank.Play2D(KEY_BOUNCE_SOUND);
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
    
    public void PlayFx8()
    {
        fx8.Play();
    }

    public void PlaySound1() => bank.Play2D(keySound01);
    public void PlaySound2() => bank.Play2D(keySound02);
    public void PlaySound3() => bank.Play2D(keySound03);
    public void PlaySound4() => bank.Play2D(keySound04);
    public void PlaySound5() => bank.Play2D(keySound05);
}
