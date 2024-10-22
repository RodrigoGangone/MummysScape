using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSplash : MonoBehaviour
{
    [SerializeField] private ParticleSystem _waterSplashFX;

    private Vector3 _offset = new(0, -0.5f, 0);

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box") ||
            other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("Pickable"))
        {
            _waterSplashFX.transform.position = other.transform.position + _offset;

            _waterSplashFX.Play();
        }
    }
}