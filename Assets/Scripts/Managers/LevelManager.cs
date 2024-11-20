using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{
    private Player _player;

    [SerializeField] public float _currentTimeDeath;
    [SerializeField] public float _maxTimeDeath = 30f;
    [SerializeField] private float _speedRecovery;

    private bool _isWin;
    private bool _isLose;
    private bool _canPause = true;

    internal bool isBusy; //En caso de que el craneo este usando alguna accion, evito que muera por tiempo

    [SerializeField] private List<Collectible> _collectibles = new();

    private List<CollectibleNumber> _collectibleNumbers = new();

    private int nextSceneLoad;

    public LevelState _currentLevelState = LevelState.Playing;

    public Action OnPlayerWin;
    public Action OnPlayerDeath;
    public Action OnPlaying;
    public Action OnPause;

    public Action DeathTimer;

    public Action<CollectibleNumber> AddCollectible;

    private Coroutine _deathTimerCoroutine;

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        nextSceneLoad = SceneManager.GetActiveScene().buildIndex + 1; //Siguiente lvl

        ValidateCollectibleInScene();

        OnPlayerWin += () =>
        {
            Win();
            StopTimerDeath();
        };

        OnPlayerDeath += () =>
        {
            Lose();
            StopTimerDeath();
        };

        OnPlaying += ActivePlayer;
        OnPlaying += VerifyPause;
        OnPlaying += () => { _player._anim.enabled = true; };

        OnPause += DesActivePlayer;
        OnPause += VerifyPause;
        OnPause += () => { _player._anim.enabled = false; };

        AddCollectible += CollectibleCount;

        _currentTimeDeath = _maxTimeDeath;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !UIManager.PauseCharging && _canPause)
        {
            if (_currentLevelState == LevelState.Playing)
                OnPause?.Invoke();
            else
                OnPlaying?.Invoke();
        }

        if (!_isWin && !_isLose && !isBusy)
        {
            if (_player.CurrentPlayerSize == PlayerSize.Head && _deathTimerCoroutine == null)
            {
                _deathTimerCoroutine = StartCoroutine(DeathTimerCoroutine());
                DeathTimer.Invoke();
            }
            else if (_player.CurrentPlayerSize != PlayerSize.Head && _deathTimerCoroutine != null)
            {
                StopCoroutine(_deathTimerCoroutine);
                _deathTimerCoroutine = null;

                StartCoroutine(ResetDeathTimer());
            }
        }
    }

    //TODO: ACA DEBERIAMOS UNIFICAR ESTE METODO CON EL VALIDATEGEMS DEL UI MANAGER, SON DOS METODOS QUE REALIZAN CASI
    //TODO: LA MISMA ACCION, POR LO TANTO DEBERIAN ESTAR UNIFICADOS [EN UN FUTURO SE PODRIA UTILIZAR PARA MANTENER LAS
    //TODO: PLATAFORMAS QUE ACTIVE CON ANTERIORIDAD]
    private void ValidateCollectibleInScene()
    {
        _collectibles.AddRange(FindObjectsOfType<Collectible>());

        foreach (var collectible in _collectibles)
        {
            Renderer renderer = collectible.GetComponentInChildren<Renderer>();
            renderer.material.SetFloat("_IsPicked", 1);
        }

        foreach (var level in LevelManagerJson.Levels)
        {
            if (level.level.Equals(SceneManager.GetActiveScene().buildIndex))
            {
                foreach (var collectibleNumber in level.collectibleNumbers)
                {
                    foreach (var collectible in _collectibles)
                    {
                        if (collectible.CollectibleNumber == collectibleNumber)
                        {
                            collectible.GetComponentInChildren<Renderer>().material.SetFloat("_IsPicked", 0);
                        }
                    }
                }
            }
        }
    }

    public IEnumerator DeathTimerCoroutine()
    {
        while (_currentTimeDeath > 0)
        {
            if (_currentLevelState == LevelState.Playing)
            {
                _currentTimeDeath -= Time.deltaTime;
            }

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

    public void DesActivePlayer()
    {
        _player.enabled = false;
        _player._rigidbody.isKinematic = true;
    }

    public void ActivePlayer()
    {
        _player._rigidbody.isKinematic = false;
        _player.enabled = true;
    }

    private void VerifyPause()
    {
        _currentLevelState = _currentLevelState.Equals(LevelState.Pause)
            ? LevelState.Playing
            : LevelState.Pause;
    }

    private void CollectibleCount(CollectibleNumber collectible)
    {
        _collectibleNumbers.Add(collectible);
    }

    public void StopTimerDeath()
    {
        if (_deathTimerCoroutine == null) return;

        StopCoroutine(_deathTimerCoroutine);
        _deathTimerCoroutine = null;
    }

    private void Win()
    {
        Debug.Log("WIIIIIIIIIIIIIIIN");

        _isWin = true;
        
        AudioManager.Instance.PlayMusic(NameSounds.Music_Win);

        LevelManagerJson.AddNewLevel(SceneManager.GetActiveScene().buildIndex,
            _collectibleNumbers,
            0f);

        LevelManagerJson.SHOWPREFLEVELS();

        _canPause = false;
    }

    private void Lose()
    {
        Debug.Log("LOOOOOOOOOOOOOOOOOOOOOOOOOOOSSSSSSSSEEEEEEEEE");
        _isLose = true;
        
        AudioManager.Instance.PlayMusic(NameSounds.Music_Lose);

        _canPause = false;
    }
}

public enum LevelState
{
    Playing,
    Pause
}