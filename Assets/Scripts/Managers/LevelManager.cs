using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private Player _player;
    private UIManager _uiManager;

    [SerializeField] public float _currentTimeDeath;
    [SerializeField] public float _maxTimeDeath = 30f;
    [SerializeField] private float _speedRecovery;

    [SerializeField] private int _collectibleCount;

    private LevelState _currentLevelState = LevelState.Playing;

    [SerializeField] private GameObject _portalFxOff;
    [SerializeField] private GameObject _portalFxOn;

    public Action OnPlayerWin;
    public Action OnPlayerDeath;

    public Action OnPlaying;
    public Action OnPause;

    private Coroutine _deathTimerCoroutine;

    private void Start()
    {
        _player = FindObjectOfType<Player>();
        _uiManager = FindObjectOfType<UIManager>();

        OnPlayerWin += Win;
        OnPlayerDeath += Lose;

        OnPlaying += HandlePlay;
        OnPlaying += VerifyPause;

        OnPause += HandlePause;
        OnPause += VerifyPause;

        _currentTimeDeath = _maxTimeDeath;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) //ESTO LO DEBERIA MANEJAR EL LEVEL MANAGER
        {
            if (_currentLevelState == LevelState.Playing)
                OnPause?.Invoke();
            else
                OnPlaying?.Invoke();
        }

        if (_player.CurrentPlayerSize == PlayerSize.Head && _deathTimerCoroutine == null)
        {
            _deathTimerCoroutine = StartCoroutine(DeathTimerCoroutine());
        }
        else if (_player.CurrentPlayerSize != PlayerSize.Head && _deathTimerCoroutine != null)
        {
            StopCoroutine(_deathTimerCoroutine);
            _deathTimerCoroutine = null;

            StartCoroutine(ResetDeathTimer());
        }
    }

    private IEnumerator DeathTimerCoroutine()
    {
        while (_currentTimeDeath > 0)
        {
            _currentTimeDeath -= Time.deltaTime;
            yield return null;
        }

        ChangeState(LevelState.Lost);
    }

    private IEnumerator ResetDeathTimer()
    {
        while (_currentTimeDeath < _maxTimeDeath)
        {
            _currentTimeDeath += Time.deltaTime * _speedRecovery;
            yield return null;
        }

        _currentTimeDeath = _maxTimeDeath;
    }

    private void HandlePause()
    {
        _player.enabled = false;
    }

    private void HandlePlay()
    {
        _player.enabled = true;
    }

    private void VerifyPause()
    {
        _currentLevelState = _currentLevelState.Equals(LevelState.Pause)
            ? LevelState.Playing
            : LevelState.Pause;
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
    Pause,
    Won,
    Lost
}