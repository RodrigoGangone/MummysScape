using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSplash : MonoBehaviour
{
    [SerializeField] private ParticleSystem _waterSplashFX;

    private PlayerController _player;
    private readonly Vector3 _offset = new(0, -0.5f, 0);

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box") ||
            other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("Pickable"))
        {
            _waterSplashFX.transform.position = other.transform.position + _offset;

            _waterSplashFX.Play();
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Pickable")) other.gameObject.SetActive(false);

        //if (!other.gameObject.CompareTag("PlayerFather")) return;
        //
        //_player = other.GetComponent<PlayerController>();
        //
        //if (_player.Ctx.Model.Size != 0)
        //    _player.Ctx.Model.TryConsumeBandage(_player.Ctx.Model.Bandages);
    }
}