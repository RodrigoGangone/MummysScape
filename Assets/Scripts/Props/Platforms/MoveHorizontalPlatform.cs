using System.Collections;
using UnityEngine;
using static PauseUtils;

/// <summary>
/// Plataforma Horizontal:
/// - Idle: detenida
/// - Activating: espera la llegada del foco
/// - MovingToStart: vuelve/va al primer waypoint antes del ciclo normal
/// - Moving: se desplaza entre waypoints
/// - Waiting: espera al llegar a un waypoint
///
/// Pause/Lock no cambian el estado lógico: solo congelan ejecución y feedback.
/// </summary>
public class MoveHorizontalPlatform : MonoBehaviour, IPausable, IFocusActivatablePlatform
{
    private enum PlatformState
    {
        Idle,
        Activating,
        MovingToStart,
        Moving,
        Waiting
    }

    [Header("PLAY ON AWAKE")]
    [SerializeField] private bool isMoving;

    [Header("SPEED")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stopTime = 0.5f;

    [Header("WAYPOINTS")]
    [SerializeField] private Transform[] waypoints;

    [Header("AUDIO & FX")]
    [SerializeField] private FxBank platformBank;
    [SerializeField] private string movingSoundKey = "Moving";
    [SerializeField] private string activeSoundKey = "Active";
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float glowIntensity = 2f;

    [Header("SAND MOUNDS EFFECTS")]
    [SerializeField] private float moundEmergenceSpeed = 1f;
    [SerializeField] private Transform sandMoundForward;
    [SerializeField] private Transform[] sandMoundForwardWaypoints;
    [SerializeField] private Transform sandMoundBackward;
    [SerializeField] private Transform[] sandMoundBackwardWaypoints;
    [SerializeField] private ParticleSystem[] sandMoundForwardParticles;
    [SerializeField] private ParticleSystem[] sandMoundBackwardParticles;
    [SerializeField] private ParticleSystem activationParticles;

    private PlatformState _state = PlatformState.Idle;

    private ActivationGlow _activationGlow;
    private AudioSource _movingAudio;
    private Coroutine _stateRoutine;
    private FocusOnActivation _focusOnActivation;
    private FocusManager.ActivationHandle _activation;
    private Coroutine _glowRoutine;
    private bool _activationEffectsStarted;
    private bool _activationRumblePending;
    public bool IsPreparingActivation => isActiveAndEnabled && _state == PlatformState.Activating;
    private bool _moveDuringFocus;

    private int _currentWaypointIndex;

    private bool _paused;
    private bool _isLocked;

    private bool HasValidWaypoints => waypoints != null && waypoints.Length > 0;
    private bool HasCycleWaypoints => waypoints != null && waypoints.Length > 1;
    private bool IsFrozen => _paused || (_moveDuringFocus && PlayerLock.Instance != null
        ? PlayerLock.Instance.IsLockedExcept(FocusManager.LockId) : _isLocked);
    private bool CanMoveLogic => !IsFrozen && HasValidWaypoints;

    private void Awake()
    {
        _activationGlow = GetComponent<ActivationGlow>();
        if (_activationGlow == null) _activationGlow = gameObject.AddComponent<ActivationGlow>();
        _focusOnActivation = GetComponent<FocusOnActivation>();
    }

    private void Start()
    {
        if (!HasValidWaypoints)
        {
            Debug.LogWarning("MoveHorizontalPlatform no tiene waypoints asignados. Se desactivará.", this);
            enabled = false;
            return;
        }

        if (Vector3.Distance(transform.position, waypoints[0].position) > 0.001f)
            transform.position = waypoints[0].position;

        _currentWaypointIndex = 0;

        InitializeSandMounds();

        if (isMoving && HasCycleWaypoints)
        {
            _currentWaypointIndex = 1;
            ChangeState(PlatformState.Moving);
        }
        else
        {
            ChangeState(PlatformState.Idle);
        }

        RefreshFeedback();
    }

    private void Update()
    {
        if (!HasValidWaypoints)
            return;

        switch (_state)
        {
            case PlatformState.MovingToStart:
                TickMoveToStart();
                break;

            case PlatformState.Moving:
                TickMove();
                break;
        }

        RefreshFeedback();
        UpdateSandMounds();
    }

    #region Public API

    public void StartAction()
    {
        if (!isActiveAndEnabled || !HasCycleWaypoints)
            return;

        switch (_state)
        {
            case PlatformState.Idle:
                BeginActivation();
                break;

            case PlatformState.Activating:
            case PlatformState.MovingToStart:
            case PlatformState.Moving:
            case PlatformState.Waiting:
                StopPlatform();
                break;
        }
    }

    public void ReturnToPrevious()
    {
        if (!HasCycleWaypoints)
            return;

        CancelActivation();
        StopCurrentRoutine();

        _currentWaypointIndex = GetPreviousIndex(_currentWaypointIndex);
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
        if (_focusOnActivation != null)
            _activation = _focusOnActivation.ActivateWhenFocused(this, BeginActivationEffects, EndActivationFocus,
                () => IsPreparingActivation);
        else
            BeginActivationEffects(false);
    }

    public void StartActionWithoutFocus(bool allowMovementDuringFocus)
    {
        if (!isActiveAndEnabled || !HasCycleWaypoints) return;
        if (_state != PlatformState.Idle) { StopPlatform(); return; }
        ChangeState(PlatformState.Activating);
        BeginActivationEffects(allowMovementDuringFocus);
    }

    public void EndActivationFocus()
    {
        _moveDuringFocus = false;
        if (_state == PlatformState.Activating && !_activationEffectsStarted) ChangeState(PlatformState.Idle);
        RefreshFeedback();
    }

    private void CancelActivation()
    {
        _activationEffectsStarted = false;
        _activationRumblePending = false;
        _activation?.Dispose();
        _activation = null;
        EndActivationFocus();
        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        _glowRoutine = null;
        activationParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetGlow(0f);
    }

    private void StopPlatform()
    {
        CancelActivation();
        StopCurrentRoutine();
        isMoving = false;
        ChangeState(PlatformState.Idle);
    }

    private void ChangeState(PlatformState newState)
    {
        _state = newState;
        RefreshFeedback();
    }

    private void BeginActivationEffects(bool hasFocus)
    {
        if (!isActiveAndEnabled || _state != PlatformState.Activating) return;
        _moveDuringFocus = hasFocus;
        _activationEffectsStarted = true;
        _glowRoutine = StartCoroutine(ActivationEffectsRoutine());
    }

    private IEnumerator ActivationEffectsRoutine()
    {
        while (IsFrozen) yield return null;
        activationParticles?.Play(true);
        yield return RunGlowSequence();
        if (activationParticles != null)
        {
            activationParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            while (activationParticles.IsAlive(true) || IsFrozen) yield return null;
        }
        while (IsFrozen) yield return null;
        if (!isActiveAndEnabled || _state != PlatformState.Activating) yield break;
        _activationEffectsStarted = false;
        _activationRumblePending = true;
        BeginMovement();
        _glowRoutine = null;
    }

    // Un pulso con el primer desplazamiento, nunca al cambiar de waypoint.
    private void NotifyActivationMovement(Vector3 previousPosition)
    {
        if (!_activationRumblePending || transform.position == previousPosition) return;
        _activationRumblePending = false;
        if (GameEventManager.Instance == null) return;
        GameEventManager.Instance.levelEvents.OnRumbleHigh.Raise(0.8f, 2f);
        GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(0.8f, 2f);
    }

    private void BeginMovement()
    {
        if (!isActiveAndEnabled || _state != PlatformState.Activating) return;
        isMoving = true;

        if (Vector3.Distance(transform.position, waypoints[0].position) > 0.001f)
        {
            ChangeState(PlatformState.MovingToStart);
        }
        else
        {
            if (_currentWaypointIndex == 0)
                _currentWaypointIndex = 1;

            ChangeState(PlatformState.Moving);
        }

        RefreshFeedback();
    }

    private IEnumerator WaitAtWaypointRoutine()
    {
        ChangeState(PlatformState.Waiting);

        yield return WaitForSecondsPausable(stopTime, () => IsFrozen);

        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
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

    private void TickMoveToStart()
    {
        if (!CanMoveLogic)
            return;

        Transform firstWaypoint = waypoints[0];
        float step = speed * Time.deltaTime;

        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, firstWaypoint.position, step);
        NotifyActivationMovement(previousPosition);

        if (Vector3.Distance(transform.position, firstWaypoint.position) <= 0.001f)
        {
            transform.position = firstWaypoint.position;
            _currentWaypointIndex = HasCycleWaypoints ? 1 : 0;
            ChangeState(HasCycleWaypoints ? PlatformState.Moving : PlatformState.Idle);
        }
    }

    private void TickMove()
    {
        if (!CanMoveLogic)
            return;

        Transform targetWaypoint = waypoints[_currentWaypointIndex];
        float step = speed * Time.deltaTime;

        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, step);
        NotifyActivationMovement(previousPosition);

        if (Vector3.Distance(transform.position, targetWaypoint.position) <= 0.01f)
        {
            transform.position = targetWaypoint.position;

            StopCurrentRoutine();
            _stateRoutine = StartCoroutine(WaitAtWaypointRoutine());
        }
    }

    private int GetPreviousIndex(int index)
    {
        return index > 0 ? index - 1 : waypoints.Length - 1;
    }

    #endregion

    #region Feedback

    private void RefreshFeedback()
    {
        if (activationParticles != null)
        {
            if (IsFrozen && activationParticles.isPlaying) activationParticles.Pause();
            else if (!IsFrozen && activationParticles.isPaused) activationParticles.Play();
        }
        bool shouldPlayMoveFeedback = (_state == PlatformState.Moving || _state == PlatformState.MovingToStart) && !IsFrozen;

        RefreshMovingAudio(shouldPlayMoveFeedback);
        RefreshSandParticlesPauseState();
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

    #region Sand Mounds

    private void InitializeSandMounds()
    {
        if (sandMoundForward != null && sandMoundForwardWaypoints != null && sandMoundForwardWaypoints.Length > 0)
            sandMoundForward.position = sandMoundForwardWaypoints[0].position;

        if (sandMoundBackward != null && sandMoundBackwardWaypoints != null && sandMoundBackwardWaypoints.Length > 0)
            sandMoundBackward.position = sandMoundBackwardWaypoints[0].position;
    }

    private void UpdateSandMounds()
    {
        if (sandMoundForward == null || sandMoundBackward == null)
            return;

        bool shouldMoveMounds = (_state == PlatformState.Moving || _state == PlatformState.MovingToStart) && !IsFrozen;

        if (shouldMoveMounds)
            MoveSandMounds();
        else
            ResetSandMoundPositions();
    }

    private void ResetSandMoundPositions()
    {
        if (sandMoundForward != null && sandMoundForwardWaypoints != null && sandMoundForwardWaypoints.Length > 0)
        {
            sandMoundForward.position = Vector3.MoveTowards(
                sandMoundForward.position,
                sandMoundForwardWaypoints[0].position,
                moundEmergenceSpeed * Time.deltaTime);
        }

        if (sandMoundBackward != null && sandMoundBackwardWaypoints != null && sandMoundBackwardWaypoints.Length > 0)
        {
            sandMoundBackward.position = Vector3.MoveTowards(
                sandMoundBackward.position,
                sandMoundBackwardWaypoints[0].position,
                moundEmergenceSpeed * Time.deltaTime);
        }
    }

    private void MoveSandMounds()
    {
        if (!CanUseSandMoundsForCurrentIndex())
            return;

        Transform targetForward = sandMoundForwardWaypoints[_currentWaypointIndex];
        int inverseIndex = (sandMoundForwardWaypoints.Length - 1) - _currentWaypointIndex;
        Transform targetBackward = sandMoundBackwardWaypoints[inverseIndex];

        sandMoundForward.position = Vector3.MoveTowards(
            sandMoundForward.position,
            targetForward.position,
            moundEmergenceSpeed * Time.deltaTime);

        sandMoundBackward.position = Vector3.MoveTowards(
            sandMoundBackward.position,
            targetBackward.position,
            moundEmergenceSpeed * Time.deltaTime);

        UpdateSandMoundDirectionalParticles();
    }

    private bool CanUseSandMoundsForCurrentIndex()
    {
        if (sandMoundForwardWaypoints == null || sandMoundBackwardWaypoints == null)
            return false;

        if (_currentWaypointIndex < 0 || _currentWaypointIndex >= sandMoundForwardWaypoints.Length)
            return false;

        int inverseIndex = (sandMoundForwardWaypoints.Length - 1) - _currentWaypointIndex;
        if (inverseIndex < 0 || inverseIndex >= sandMoundBackwardWaypoints.Length)
            return false;

        return true;
    }

    private void UpdateSandMoundDirectionalParticles()
    {
        bool playForward = _currentWaypointIndex == 1;

        if (playForward)
        {
            PlayParticleArray(sandMoundForwardParticles);
            StopParticleArray(sandMoundBackwardParticles);
        }
        else
        {
            PlayParticleArray(sandMoundBackwardParticles);
            StopParticleArray(sandMoundForwardParticles);
        }
    }

    private void RefreshSandParticlesPauseState()
    {
        bool isFrozen = IsFrozen;
        HandleParticlesPause(sandMoundForwardParticles, isFrozen);
        HandleParticlesPause(sandMoundBackwardParticles, isFrozen);
    }

    private void PlayParticleArray(ParticleSystem[] systems)
    {
        if (systems == null || IsFrozen)
            return;

        foreach (var ps in systems)
        {
            if (ps == null) continue;
            if (!ps.isPlaying)
                ps.Play();
        }
    }

    private void StopParticleArray(ParticleSystem[] systems)
    {
        if (systems == null)
            return;

        foreach (var ps in systems)
        {
            if (ps == null) continue;
            if (ps.isPlaying)
                ps.Stop();
        }
    }

    private void HandleParticlesPause(ParticleSystem[] systems, bool isFrozen)
    {
        if (systems == null) return;

        foreach (var ps in systems)
        {
            if (ps == null) continue;

            if (isFrozen && ps.isPlaying)
                ps.Pause();
            else if (!isFrozen && ps.isPaused)
                ps.Play();
        }
    }

    #endregion

    #region Glow

    private IEnumerator RunGlowSequence()
    {
        while (IsFrozen) yield return null;
        yield return AnimateGlow(0f, glowIntensity, glowDuration * 0.5f);

        if (platformBank != null && !string.IsNullOrEmpty(activeSoundKey))
            platformBank.Play3D(activeSoundKey, transform.position);

        yield return AnimateGlow(glowIntensity, 0f, glowDuration * 0.5f);

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
            while (IsFrozen)
                yield return null;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float current = Mathf.Lerp(from, to, t);
            SetGlow(current);
            yield return null;
        }

        SetGlow(to);
    }

    private void SetGlow(float intensity) => _activationGlow?.SetIntensity(intensity);

    #endregion

    #region Pause & Lock

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;
        RefreshFeedback();
    }

    public void OnLockChanged(bool locked)
    {
        _isLocked = locked;
        RefreshFeedback();
    }

    private void OnEnable()
    {
        _isLocked = PlayerLock.Instance != null && PlayerLock.Instance.IsLocked;
        if (GameEventManager.Instance == null) return;
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
    }

    private void OnDisable()
    {
        StopPlatform();
        if (GameEventManager.Instance == null) return;

        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);
    }

    #endregion

    // #region Player Collision Logic
    //
    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //         other.transform.SetParent(transform);
    // }
    //
    // private void OnTriggerExit(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //         other.transform.SetParent(null);
    // }
    //
    // #endregion
}

