using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utils;

public class LevelManager : MonoBehaviour
{
    private Player _player;

    [SerializeField] public float _currentTimeDeath;
    [SerializeField] public float _maxTimeDeath = 30f;
    [SerializeField] private float _speedRecovery;

    [SerializeField] private int _collectibleCount;

    private int nextSceneLoad;

    private LevelState _currentLevelState = LevelState.Playing;

    public Action OnPlayerWin;
    public Action OnPlayerDeath;
    public Action OnPlaying;
    public Action OnPause;
    
    public Action<CollectibleNumber> AddCollectible;

    private Coroutine _deathTimerCoroutine;

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        nextSceneLoad = SceneManager.GetActiveScene().buildIndex + 1; //Siguiente lvl

        OnPlayerWin += Win;
        OnPlayerDeath += Lose;

        OnPlaying += HandlePlay;
        OnPlaying += VerifyPause;

        OnPause += HandlePause;
        OnPause += VerifyPause;

        AddCollectible += CollectibleCount;

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

        OnPlayerDeath?.Invoke();
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

    private void CollectibleCount(CollectibleNumber num)
    {
        _collectibleCount++;
    }

    private void Win()
    {
        PlayerPrefsHandler.SaveLevelAt(nextSceneLoad);
        Debug.Log($"LevelManager -> PlayerPref: LVL_AT {nextSceneLoad}");
        Debug.Log("LevelManager -> Ganaste!");
    }

    private void Lose()
    {
        Debug.Log("LevelManager -> Perdiste!");
    }
}

public enum LevelState
{
    Playing,
    Pause
}