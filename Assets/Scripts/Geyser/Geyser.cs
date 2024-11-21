using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Geyser : MonoBehaviour
{
    private Player _player;

    [FormerlySerializedAs("geyserType")] [SerializeField]
    GeyserType _currentGeyserType;

    [SerializeField] private Transform _viewBasic;
    [SerializeField] private Transform _viewIntense;

    [SerializeField] private Transform _invisiblePlatform;
    [SerializeField] private Transform _triggerTransform;

    private bool _isPaused;
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

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        Transform selectedView = _currentGeyserType == GeyserType.Intense ? _viewIntense : _viewBasic;
        _viewIntense.gameObject.SetActive(selectedView == _viewIntense);
        _viewBasic.gameObject.SetActive(selectedView == _viewBasic);
    }

    private void Update()
    {
        if (_currentGeyserType == GeyserType.Basic)
        {
            MoveTowardsWaypoint();
        }

        if (_upInvisiblePlatform)
            UpInvisiblePlatform(_currentGeyserType == GeyserType.Intense ? _viewIntense : _viewBasic);
    }

    #region Basic Mode => Common use

    private void MoveTowardsWaypoint()
    {
        if (_isPaused)
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
        _isPaused = true;

        if (_currentWaypointIndex == 0)
        {
            yield return new WaitForSeconds(stopTimeBase / 2);
            _preUp1.Play();
            _preUp2.Play();
            yield return new WaitForSeconds(stopTimeBase / 2);
        }
        else
        {
            yield return new WaitForSeconds(stopTimeTop);
            _preUp1.Stop();
            _preUp2.Stop();
        }

        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;

        _isPaused = false;
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
            _viewIntense.position = Vector3.MoveTowards(_viewIntense.position, waypoints[1].position,
                intenseSpeed * Time.deltaTime);
            _triggerTransform.position = new Vector3(
                _triggerTransform.position.x,
                _viewIntense.position.y,
                _triggerTransform.position.z);
            yield return null;
        }

        yield return new WaitForSeconds(stoptimeTopIntense);

        while (Vector3.Distance(_viewIntense.position, waypoints[0].position) > 0.01f)
        {
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

        if (_player.CurrentPlayerSize != PlayerSize.Head)
        {
            _player._modelPlayer.CountBandage(-_player.CurrentBandageStock);
        }
    }

    public void OnPlayerExitTrigger(Collider player)
    {
        player.transform.SetParent(null);
        _upInvisiblePlatform = false;

        _invisiblePlatform.position = waypoints[0].position;
    }
}

enum GeyserType
{
    Basic,
    Intense
}