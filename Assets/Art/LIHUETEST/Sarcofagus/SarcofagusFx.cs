using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SarcofagusFx : MonoBehaviour
{
    [SerializeField] private ParticleSystem fx1;
    [SerializeField] private ParticleSystem fx2;
    [SerializeField] private ParticleSystem fx3;
    [SerializeField] private ParticleSystem fx4;

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
}
