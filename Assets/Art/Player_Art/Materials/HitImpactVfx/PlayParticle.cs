using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayParticle : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitImpact, _puff;
    [SerializeField] private KeyCode _ImpactKey = KeyCode.I,
                                     _puffKey   = KeyCode.P;

    private void Update()
    {
        if(Input.GetKeyDown(_ImpactKey))
        {
            _hitImpact.Play();
        }

        if (Input.GetKeyDown(_puffKey))
        {
            _puff.Play();
        }
    }
}
