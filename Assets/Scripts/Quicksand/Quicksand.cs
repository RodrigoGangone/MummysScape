using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quicksand : MonoBehaviour
{
    private Player _player;
    private LevelManager _levelManager;

    [SerializeField] private float _timeDeathNormal;
    [SerializeField] private float _timeDeathSmall;

    [SerializeField] private GameObject _sinkFX;

    private bool _inQuicksand;
    private float _timeToDeath;


    private void Start()
    {
        _player = FindObjectOfType<Player>();
        _levelManager = FindObjectOfType<LevelManager>();

        _levelManager.OnPlayerDeath += SinkPlayer;
    }

    private void Update()
    {
        if (!_inQuicksand) return;

        _timeToDeath += Time.deltaTime;

        if (_timeToDeath > _timeDeathNormal)
        {
            _levelManager.OnPlayerDeath?.Invoke();
            enabled = false;
        }
    }

    private void OnCollisionStay(Collision other)
    {
        if (!other.gameObject.CompareTag(Utils.PLAYER_TAG)) return;

        if (_player.CurrentPlayerSize != PlayerSize.Head)
            _inQuicksand = true;
    }

    private void OnCollisionExit(Collision other)
    {
        if (!other.gameObject.CompareTag(Utils.PLAYER_TAG)) return;

        _inQuicksand = false;
        _timeToDeath = 0;
    }

    private void SinkPlayer()
    {
        Instantiate(_sinkFX, _player.transform.position, _player.transform.rotation);
        enabled = false;
    }
}