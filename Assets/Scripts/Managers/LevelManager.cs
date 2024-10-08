using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    private Player _player;

    [SerializeField] public float _currentTimeDeath;
    [SerializeField] public float _maxTimeDeath = 30f;
    [SerializeField] private float _speedRecovery;

    [SerializeField] private List<Collectible> _collectibles = new();

    private List<CollectibleNumber> _collectibleNumbers = new();

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

        ValidateCollectibleInScene();

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

    private void CollectibleCount(CollectibleNumber collectible)
    {
        _collectibleNumbers.Add(collectible);
    }

    private void Win()
    {
        LevelManagerJson.AddNewLevel(SceneManager.GetActiveScene().buildIndex,
            _collectibleNumbers,
            0f);

        LevelManagerJson.SHOWPREFLEVELS();
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