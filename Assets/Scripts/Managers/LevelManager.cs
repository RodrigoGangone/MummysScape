using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private Player _player;
    private UIManager _uiManager;

    [SerializeField] public float _currentTimeDeath;
    [SerializeField] public float _maxTimeDeath = 30f;

    [SerializeField] private int _collectibleCount;

    private LevelState _currentLevelState = LevelState.Playing;

    [SerializeField] private GameObject _portalFxOff;
    [SerializeField] private GameObject _portalFxOn;

    public Action OnPlayerWin;
    public Action OnPlayerDeath;

    private void Start()
    {
        _player = FindObjectOfType<Player>();
        _uiManager = FindObjectOfType<UIManager>();

        OnPlayerWin += Win;
        OnPlayerDeath += Lose;
    }

    private void Update()
    {
        if (_currentLevelState == LevelState.Playing)
            HandleGameplay();
    }

    private void HandleGameplay()
    {
        if (_currentTimeDeath >= _maxTimeDeath && _player.CurrentPlayerSize != PlayerSize.Head) return;

        SetTimerDeath(_player.CurrentPlayerSize);

        _uiManager.UISetTimerDeath(_currentTimeDeath, _maxTimeDeath);

        if (_currentTimeDeath <= 0)
            ChangeState(LevelState.Lost);
    }

    private void SetTimerDeath(PlayerSize playerSize)
    {
        if (playerSize == PlayerSize.Head)
        {
            _currentTimeDeath -= Time.deltaTime;
            //_uiManager.SetMaterialUI(_currentTimeDeath <= _maxTimeDeath / 2 ? _currentTimeDeath : 0);
        }
        else
            _currentTimeDeath += Time.deltaTime * 30f;
    }

    public void CollectibleCount(int sum, CollectibleNumber num)
    {
        _collectibleCount += sum;

        _uiManager.UISetCollectibleCount(num);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        ChangeState(LevelState.Won);

        _portalFxOff.SetActive(false);
        _portalFxOn.SetActive(true);
    }

    private void ChangeState(LevelState newState)
    {
        if (_currentLevelState == newState) return;

        _currentLevelState = newState;

        switch (newState)
        {
            case LevelState.Won:
                OnPlayerWin?.Invoke();
                break;

            case LevelState.Lost:
                OnPlayerDeath?.Invoke();
                break;
        }
    }

    private void Win()
    {
        Debug.Log("Ganaste!");
    }

    private void Lose()
    {
        Debug.Log("Perdiste!");
    }
}

public enum LevelState
{
    Playing,
    Won,
    Lost
}