using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayParticle : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitImpact;
    [SerializeField] private KeyCode _ImpactKey = KeyCode.I;

    private void Update()
    {
        if(Input.GetKeyDown(_ImpactKey))
        {
            _hitImpact.Play();
        }
    }
}
