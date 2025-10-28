using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSplash : MonoBehaviour
{
    [SerializeField] private ParticleSystem _waterSplashFX;

    private Player _player;

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
        
        if(other.gameObject.layer == LayerMask.NameToLayer("Pickable")) other.gameObject.SetActive(false);

        if (other.gameObject.CompareTag("PlayerFather"))
        {
            _player = other.GetComponent<Player>();

            if (_player.CurrentBandageStock != 0)
                _player._modelPlayer.CountBandage(-_player.CurrentBandageStock);
        }
    }
}