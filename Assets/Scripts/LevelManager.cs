using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem _fxPortal;

    [SerializeField] private float _timeDeath = 30f;

    private UIManager _uiManager;

    public Action playerWin;
    public Action playerDeath;

    public Player _player;

    private void Start()
    {
        _player = FindObjectOfType<Player>();
        _uiManager = FindObjectOfType<UIManager>();
    }

    private void Update()
    {
        if (_timeDeath >= 30f && _player.CurrentPlayerSize != PlayerSize.Head) return;

        SetTimerDeath(_player.CurrentPlayerSize);

        _uiManager.UISetTimerDeath(_timeDeath);

        if (_timeDeath <= 0)
            playerDeath?.Invoke();
    }

    private void SetTimerDeath(PlayerSize playerSize)
    {
        if (playerSize == PlayerSize.Head)
            _timeDeath -= Time.deltaTime;
        else
            _timeDeath += Time.deltaTime * 10f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _fxPortal.Play();
        playerWin?.Invoke();
    }
}