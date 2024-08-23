using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem _fxPortal;

    [SerializeField] private float _currentTimeDeath;
    [SerializeField] private float _maxTimeDeath = 30f;

    private UIManager _uiManager;

    private List<Collectible> _collectibles = new();

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
        if (_currentTimeDeath >= _maxTimeDeath && _player.CurrentPlayerSize != PlayerSize.Head) return;

        SetTimerDeath(_player.CurrentPlayerSize);

        _uiManager.UISetTimerDeath(_currentTimeDeath, _maxTimeDeath);

        if (_currentTimeDeath <= 0)
            playerDeath?.Invoke();
    }

    private void SetTimerDeath(PlayerSize playerSize)
    {
        if (playerSize == PlayerSize.Head)
        {
            _currentTimeDeath -= Time.deltaTime;
            _uiManager.SetMaterialUI(_currentTimeDeath <= _maxTimeDeath / 2 ? _currentTimeDeath : 0);
        }
        else
            _currentTimeDeath += Time.deltaTime * 30f;
        
    }

    public void AddCollectible(Collectible beetle)
    {
        _collectibles.Add(beetle);
        _uiManager.UISetCollectibleCount(_collectibles.Count);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _fxPortal.Play();
        playerWin?.Invoke();
    }
}