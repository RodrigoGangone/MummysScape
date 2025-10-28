using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using static PlayerEnum;
using static PauseUtils;

public class Geyser : MonoBehaviour, IPausable
{
    private PlayerContext _playerContext;

    [FormerlySerializedAs("geyserType")] [SerializeField]
    GeyserType _currentGeyserType;

    [SerializeField] private Transform _viewBasic;
    [SerializeField] private Transform _viewIntense;

    [SerializeField] private Transform _invisiblePlatform;
    [SerializeField] private Transform _triggerTransform;

    private bool _isPausedPos;
    private bool _upInvisiblePlatform;
    public bool _isIntenseModeActive;

    [Header("SPEED BASIC")] [SerializeField]
    private float speedSand = 3;

    [SerializeField] private float speedInvisiblePlatform = 5;
    [SerializeField] private float stopTimeBase = 3f;
    [SerializeField] private float stopTimeTop = 3f;

    [Header("SPEED INTENSE")] [SerializeField]
    private float stoptimeTopIntense = 3f;

    [SerializeField] private float intenseSpeed = 10f;

    [Header("WAYPOINTS")] [SerializeField] private Transform[] waypoints;
    private int _currentWaypointIndex;

    [Header("WAYPOINTS")] [SerializeField] private ParticleSystem _preUp1, _preUp2;
    private bool _paused;

    private void Start()
    {
        _playerContext = FindObjectOfType<PlayerController>().Ctx;

        Transform selectedView = _currentGeyserType == GeyserType.Intense ? _viewIntense : _viewBasic;
        _viewIntense.gameObject.SetActive(selectedView == _viewIntense);
        _viewBasic.gameObject.SetActive(selectedView == _viewBasic);
    }

    private void Update()
    {
        if (_paused) return; // ✅ se corta toda la lógica continua

        if (_currentGeyserType == GeyserType.Basic)
            MoveTowardsWaypoint();

        if (_upInvisiblePlatform)
            UpInvisiblePlatform(_currentGeyserType == GeyserType.Intense ? _viewIntense : _viewBasic);
    }


    #region Basic Mode => Common use

    private void MoveTowardsWaypoint()
    {
        if (_paused)
        {
            return;
        }

        // Calcula la direcc y mueve la plataform hacia el waypoint actual
        Transform targetWaypoint = waypoints[_currentWaypointIndex];
        float step = speedSand * Time.deltaTime;

        _viewBasic.transform.position =
            Vector3.MoveTowards(_viewBasic.transform.position, targetWaypoint.position, step);
        _triggerTransform.transform.position = new Vector3(_triggerTransform.transform.position.x,
            _viewBasic.transform.position.y,
            _triggerTransform.transform.position.z);

        // Pausa al llegar a un punto
        if (Vector3.Distance(_viewBasic.transform.position, targetWaypoint.position) == 0)
        {
            StartCoroutine(PauseAtWaypoint());
        }
    }

    private IEnumerator PauseAtWaypoint()
    {
        _isPausedPos = true;

        if (_currentWaypointIndex == 0)
        {
            // ⬇️ reemplazás por WaitForSecondsPausable
            yield return WaitForSecondsPausable(stopTimeBase / 2,() => _paused);
            _preUp1.Play();
            _preUp2.Play();
            yield return WaitForSecondsPausable(stopTimeBase / 2,() => _paused);
        }
        else
        {
            yield return WaitForSecondsPausable(stopTimeTop,() => _paused);
            _preUp1.Stop();
            _preUp2.Stop();
        }

        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        _isPausedPos = false;
    }


    private void UpInvisiblePlatform(Transform viewPos)
    {
        float step = speedInvisiblePlatform * Time.deltaTime;

        _invisiblePlatform.position = Vector3.MoveTowards(_invisiblePlatform.position, viewPos.position, step);
    }

    #endregion

    #region IntenseMode => Boss - Scorpion

    public void ActivateIntenseMode(Action onGeysersFinished = null)
    {
        if (_isIntenseModeActive || _currentGeyserType != GeyserType.Intense) return;

        _isIntenseModeActive = true;

        StartCoroutine(IntenseGeyserSequence(onGeysersFinished));
    }

    private IEnumerator IntenseGeyserSequence(Action onGeysersFinished = null)
    {
        while (Vector3.Distance(_viewIntense.position, waypoints[1].position) > 0.01f)
        {
            if (_paused) { yield return WaitWhilePaused(() => _paused); continue; }

            _viewIntense.position = Vector3.MoveTowards(
                _viewIntense.position, waypoints[1].position, intenseSpeed * Time.deltaTime);
            _triggerTransform.position = new Vector3(
                _triggerTransform.position.x,
                _viewIntense.position.y,
                _triggerTransform.position.z);
            yield return null;
        }

        yield return WaitForSecondsPausable(stoptimeTopIntense,() => _paused);

        while (Vector3.Distance(_viewIntense.position, waypoints[0].position) > 0.01f)
        {
            if (_paused) { yield return WaitWhilePaused(() => _paused); continue; }

            _viewIntense.position = Vector3.MoveTowards(_viewIntense.position, waypoints[0].position,
                intenseSpeed * Time.deltaTime);
            _triggerTransform.position = new Vector3(
                _triggerTransform.position.x,
                _viewIntense.position.y,
                _triggerTransform.position.z);
            yield return null;
        }

        _isIntenseModeActive = false;

        onGeysersFinished?.Invoke();
    }

    #endregion

    public void OnPlayerEnterTrigger(Collider player)
    {
        player.transform.SetParent(_invisiblePlatform);
        _upInvisiblePlatform = true;

        if (_playerContext.Model.Size != PlayerSize.Head)
        {
            _playerContext.Model.TryConsumeBandage(-_playerContext.Model.Bandages);
        }
    }

    public void OnPlayerExitTrigger(Collider player)
    {
        player.transform.SetParent(null);
        _upInvisiblePlatform = false;

        _invisiblePlatform.position = waypoints[0].position;
    }

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;

        // Partículas → Pausa/Resume si estaban activas
        if (_preUp1)
        {
            if (paused && _preUp1.isPlaying) _preUp1.Pause();
            else if (!paused && !_preUp1.isPlaying) _preUp1.Play();
        }

        if (_preUp2)
        {
            if (paused && _preUp2.isPlaying) _preUp2.Pause();
            else if (!paused && !_preUp2.isPlaying) _preUp2.Play();
        }
    }
    
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}

enum GeyserType
{
    Basic,
    Intense
}