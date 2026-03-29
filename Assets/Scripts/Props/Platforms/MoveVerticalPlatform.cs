using System.Collections;
using System.Linq;
using UnityEngine;
using static PauseUtils;

/// <summary>
/// Plataforma Vertical:
/// - Idle: detenida
/// - Activating: reproduce feedback visual/sonoro previo al movimiento
/// - Moving: se desplaza hacia el waypoint objetivo
/// - Waiting: espera en un waypoint antes de continuar
///
/// Pause/Lock no cambian el estado lógico: solo congelan la ejecución.
/// </summary>
public class MoveVerticalPlatform : MonoBehaviour, IPausable
{
    private enum PlatformState
    {
        Idle,
        Activating,
        Moving,
        Waiting
    }

    [Header("Settings")]
    [SerializeField] private bool isMovingOnStart = true;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stopTime = 0.5f;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;

    [Header("Effects")]
    [SerializeField] private FxBank platformBank;
    [SerializeField] private string movingSoundKey = "Moving";
    [SerializeField] private string activationSoundKey = "Active";
    [SerializeField] private ParticleSystem sandMoundsParticle;
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 0.15f;

    private PlatformState _state = PlatformState.Idle;

    private int _currentWaypointIndex;
    private int _targetWaypointIndex;

    private bool _isGloballyPaused;
    private bool _isLocked;

    private Material[] _platformMaterials;
    private AudioSource _movingAudio;
    private Coroutine _stateRoutine;

    private FocusOnActivation _focusOnActivation;

    private bool IsFrozen => _isGloballyPaused || _isLocked;
    private bool HasValidWaypoints => waypoints != null && waypoints.Length > 0;
    private bool CanMoveLogic => !IsFrozen && HasValidWaypoints;

    private void Awake()
    {
        _platformMaterials = GetMaterialsFromChildren();
        _focusOnActivation = GetComponent<FocusOnActivation>();
    }

    private void Start()
    {
        if (!HasValidWaypoints)
        {
            Debug.LogWarning("MoveVerticalPlatform no tiene waypoints asignados. Se desactivará.", this);
            enabled = false;
            return;
        }

        _currentWaypointIndex = 0;
        transform.position = waypoints[_currentWaypointIndex].position;

        if (isMovingOnStart && waypoints.Length > 1)
        {
            _targetWaypointIndex = GetNextIndex(_currentWaypointIndex, 1);
            ChangeState(PlatformState.Moving);
        }
        else
        {
            _targetWaypointIndex = _currentWaypointIndex;
            ChangeState(PlatformState.Idle);
        }

        RefreshFeedback();
    }

    private void Update()
    {
        if (!HasValidWaypoints)
            return;

        if (_state == PlatformState.Moving)
            TickMovement();

        RefreshFeedback();
    }

    #region Public API

    public void StartAction()
    {
        if (!HasValidWaypoints || waypoints.Length <= 1)
            return;

        switch (_state)
        {
            case PlatformState.Idle:
                BeginActivation();
                break;

            case PlatformState.Moving:
            case PlatformState.Waiting:
            case PlatformState.Activating:
                StopPlatform();
                break;
        }
    }

    public void ReturnToPrevious()
    {
        if (!HasValidWaypoints || waypoints.Length <= 1)
            return;

        StopCurrentRoutine();

        _targetWaypointIndex = GetNextIndex(_currentWaypointIndex, -1);
        ChangeState(PlatformState.Moving);
    }

    #endregion

    #region State Flow

    private void BeginActivation()
    {
        if (_state == PlatformState.Activating)
            return;

        StopCurrentRoutine();
        ChangeState(PlatformState.Activating);
        _stateRoutine = StartCoroutine(ActivationRoutine());
    }

    private void StopPlatform()
    {
        StopCurrentRoutine();
        ChangeState(PlatformState.Idle);
    }

    private void ChangeState(PlatformState newState)
    {
        _state = newState;
        RefreshFeedback();
    }

    private IEnumerator ActivationRoutine()
    {
        activationParticles?.Play();
        _focusOnActivation?.Activate();

        yield return RunGlowSequence();

        if (_state != PlatformState.Activating)
            yield break;

        _targetWaypointIndex = GetNextIndex(_currentWaypointIndex, 1);
        ChangeState(PlatformState.Moving);
        _stateRoutine = null;
    }

    private IEnumerator WaitAtWaypointRoutine()
    {
        ChangeState(PlatformState.Waiting);

        yield return WaitForSecondsPausable(stopTime, () => IsFrozen);

        _targetWaypointIndex = GetNextIndex(_currentWaypointIndex, 1);
        ChangeState(PlatformState.Moving);
        _stateRoutine = null;
    }

    private void StopCurrentRoutine()
    {
        if (_stateRoutine == null)
            return;

        StopCoroutine(_stateRoutine);
        _stateRoutine = null;
    }

    #endregion

    #region Movement

    private void TickMovement()
    {
        if (!CanMoveLogic)
            return;

        Transform target = waypoints[_targetWaypointIndex];

        float distance = Vector3.Distance(transform.position, target.position);
        const float slowDownRadius = 1f;

        float speedFactor = 1f;
        if (distance < slowDownRadius)
        {
            float t = distance / slowDownRadius;
            speedFactor = Mathf.Lerp(0.1f, 1f, t);
        }

        float step = speed * speedFactor * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        if (Vector3.Distance(transform.position, target.position) <= 0.001f)
        {
            transform.position = target.position;
            _currentWaypointIndex = _targetWaypointIndex;

            StopCurrentRoutine();
            _stateRoutine = StartCoroutine(WaitAtWaypointRoutine());
        }
    }

    private int GetNextIndex(int fromIndex, int direction)
    {
        return (fromIndex + direction + waypoints.Length) % waypoints.Length;
    }

    #endregion

    #region Feedback

    private void RefreshFeedback()
    {
        bool shouldPlayMoveFeedback = _state == PlatformState.Moving && !IsFrozen;

        RefreshSandParticles(shouldPlayMoveFeedback);
        RefreshMovingAudio(shouldPlayMoveFeedback);
    }

    private void RefreshSandParticles(bool shouldPlay)
    {
        if (sandMoundsParticle == null)
            return;

        if (shouldPlay)
        {
            if (!sandMoundsParticle.isPlaying)
                sandMoundsParticle.Play();
        }
        else
        {
            if (sandMoundsParticle.isPlaying)
                sandMoundsParticle.Stop();
        }
    }

    private void RefreshMovingAudio(bool shouldPlay)
    {
        if (shouldPlay)
        {
            EnsureMovingAudio();

            if (_movingAudio != null && !_movingAudio.isPlaying)
                _movingAudio.Play();
        }
        else
        {
            if (_movingAudio != null && _movingAudio.isPlaying)
                _movingAudio.Stop();
        }
    }

    private void EnsureMovingAudio()
    {
        if (_movingAudio != null) return;
        if (platformBank == null || string.IsNullOrEmpty(movingSoundKey)) return;

        var entry = platformBank.Get(movingSoundKey);
        if (entry == null || entry.clip == null) return;

        _movingAudio = gameObject.AddComponent<AudioSource>();
        _movingAudio.clip = entry.clip;
        _movingAudio.loop = true;
        _movingAudio.playOnAwake = false;
        _movingAudio.volume = entry.volume;
        _movingAudio.pitch = entry.pitch;
        _movingAudio.spatialBlend = entry.is3D ? entry.spatialBlend : 0f;
        _movingAudio.maxDistance = entry.maxDistance;
        _movingAudio.rolloffMode = AudioRolloffMode.Logarithmic;

        if (AudioManager.Instance != null)
        {
            var group = AudioManager.Instance.GetMixerGroup(platformBank.bus);
            if (group != null)
                _movingAudio.outputAudioMixerGroup = group;
        }
    }

    #endregion

    #region Glow

    private Material[] GetMaterialsFromChildren()
    {
        return GetComponentsInChildren<Renderer>()
            .SelectMany(r => r.materials)
            .ToArray();
    }

    private IEnumerator RunGlowSequence()
    {
        yield return AnimateGlow(0f, glowIntensity, glowDuration * 0.5f);

        if (platformBank != null && !string.IsNullOrEmpty(activationSoundKey))
            platformBank.Play3D(activationSoundKey, transform.position);

        yield return AnimateGlow(glowIntensity, 0f, glowDuration * 0.5f);

        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.levelEvents.OnRumbleHigh.Raise(0.8f, 2f);
            GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(0.8f, 2f);
        }
    }

    private IEnumerator AnimateGlow(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetGlow(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            while (_isGloballyPaused)
                yield return null;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float current = Mathf.Lerp(from, to, t);
            SetGlow(current);
            yield return null;
        }

        SetGlow(to);
    }

    private void SetGlow(float intensity)
    {
        if (_platformMaterials == null) return;

        foreach (var mat in _platformMaterials)
        {
            if (mat != null && mat.HasProperty("_GlowIntensity"))
                mat.SetFloat("_GlowIntensity", intensity);
        }
    }

    #endregion

    #region Pause & Lock

    public void OnPauseChanged(bool paused)
    {
        _isGloballyPaused = paused;
        RefreshFeedback();
    }

    public void OnLockChanged(bool locked)
    {
        _isLocked = locked;
        RefreshFeedback();
    }

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance == null) return;

        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);
    }

    #endregion

    #region Player Parenting

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(null);
    }

    #endregion
}