using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla una trampa de lanzas mediante una única secuencia redirigible: ejecuta una advertencia
/// visual cuando corresponde y mueve el cuerpo cinemático vertical junto con su collider en FixedUpdate.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpikeTrapController : MonoBehaviour, ISpikeTrapController, IPausable
{
    private enum MotionPhase
    {
        Stable,
        Activating,
        Shaking,
        Ready,
        Moving
    }

    private const float PositionEpsilonSquared = 0.0000001f;

    [Header("Peso total de los botones conectados")]
    [SerializeField, Min(1), Tooltip("Peso mínimo acumulado para media altura.")]
    private int _halfRaisedWeight = 1;
    [SerializeField, Min(2), Tooltip("Peso mínimo acumulado para bajar completamente.")]
    private int _loweredWeight = 2;

    [Header("Referencias internas")]
    [SerializeField] private Transform _motionRoot;
    [SerializeField] private Rigidbody _motionRigidbody;
    [SerializeField] private Transform _visualShakeRoot;

    [Header("Stable Local Positions")]
    [SerializeField] private Vector3 _raisedLocalPosition;
    [SerializeField] private Vector3 _halfRaisedLocalPosition;
    [SerializeField] private Vector3 _loweredLocalPosition;
    [SerializeField] private SpikeTrapState _initialState = SpikeTrapState.Raised;

    [Header("Vertical Motion")]
    [SerializeField, Min(0f)] private float _moveDuration = 0.45f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Warning Shake")]
    [SerializeField] private Vector3 _shakeLocalDirection = Vector3.right;
    [SerializeField, Min(0f)] private float _shakeAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float _shakeFrequency = 24f;
    [SerializeField, Min(0f)] private float _shakeDuration = 0.2f;

    [Header("Activation Effects")]
    [SerializeField] private Renderer[] _activationRenderers;
    [SerializeField] private ParticleSystem _activationParticles;
    [SerializeField, Min(0f)] private float _glowDuration;
    [SerializeField, Min(0f)] private float _glowIntensity = 2f;
    private ActivationGlow _activationGlow;
    private bool _grouped;
    private readonly Dictionary<PressureButtonStateResolver, int> _lastWeights = new Dictionary<PressureButtonStateResolver, int>();
    private readonly List<PressureButtonStateResolver> _changedButtons = new List<PressureButtonStateResolver>();
    private bool _paused;
    private bool _particlesStopped;

    private MotionPhase _phase;
    private Vector3 _visualShakeCenter;
    private Vector3 _moveStartLocalPosition;
    private Vector3 _moveTargetLocalPosition;
    private float _phaseElapsed;
    private bool _initialized;
    private bool _weightDriven;
    private bool _refreshWeight;

    // Una entrada por botón; cada ruta conserva su propietario para retirarla de forma independiente.
    private readonly Dictionary<PressureButtonStateResolver, HashSet<MonoBehaviour>> _buttonConnections =
        new Dictionary<PressureButtonStateResolver, HashSet<MonoBehaviour>>();
    private readonly List<PressureButtonStateResolver> _removedButtons = new List<PressureButtonStateResolver>();
    private readonly List<PressureButtonStateResolver> _contributingButtons = new List<PressureButtonStateResolver>();

    public int TotalEffectiveWeight { get; private set; }
    public IReadOnlyList<PressureButtonStateResolver> ContributingButtons => _contributingButtons;

    /// <summary>Registra una conexión sin duplicar el aporte del mismo botón.</summary>
    public void RegisterButton(PressureButtonStateResolver button, MonoBehaviour connectionOwner)
    {
        if (button == null || connectionOwner == null) return;
        if (!_buttonConnections.TryGetValue(button, out HashSet<MonoBehaviour> owners))
        {
            owners = new HashSet<MonoBehaviour>();
            _buttonConnections.Add(button, owners);
        }
        owners.Add(connectionOwner);
        button.RegisterActivationTrap(this);
        _weightDriven = true;
        _refreshWeight = true;
    }

    /// <summary>Retira sólo las conexiones creadas por este coordinador u orquestador.</summary>
    public void UnregisterButtons(MonoBehaviour connectionOwner)
    {
        foreach (var pair in _buttonConnections)
        {
            pair.Value.Remove(connectionOwner);
            if (pair.Value.Count == 0 && pair.Key != null) pair.Key.UnregisterActivationTrap(this);
        }
        _refreshWeight = true;
    }

    private void OnEnable()
    {
        _refreshWeight = true;
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    }

    private void LateUpdate()
    {
        if (!_initialized || !_weightDriven) return;
        long total = 0;
        _removedButtons.Clear();
        _contributingButtons.Clear();
        _changedButtons.Clear();
        foreach (var pair in _buttonConnections)
        {
            pair.Value.RemoveWhere(IsDestroyedOwner);
            if (pair.Key == null || pair.Value.Count == 0)
            {
                _removedButtons.Add(pair.Key);
                continue;
            }


            bool hasActiveConnection = false;
            foreach (MonoBehaviour owner in pair.Value)
                if (owner.isActiveAndEnabled) { hasActiveConnection = true; break; }
            int contribution = hasActiveConnection && pair.Key.isActiveAndEnabled ? pair.Key.TrapWeight : 0;
            if (!_lastWeights.TryGetValue(pair.Key, out int previous) || previous != contribution)
                _changedButtons.Add(pair.Key);
            _lastWeights[pair.Key] = contribution;
            if (!hasActiveConnection) continue;

            _contributingButtons.Add(pair.Key);
            total += contribution;
        }
        foreach (PressureButtonStateResolver button in _removedButtons)
        {
            if (button != null) button.UnregisterActivationTrap(this);
            _changedButtons.Add(button);
            _lastWeights.Remove(button);
            _buttonConnections.Remove(button);
        }

        int nextWeight = (int)System.Math.Min(int.MaxValue, total);
        if (!_refreshWeight && nextWeight == TotalEffectiveWeight) return;
        _refreshWeight = false;
        TotalEffectiveWeight = nextWeight;
        // Una única orden por frame, después de los cambios de sensores y temporizadores.
        var target = nextWeight >= _loweredWeight ? SpikeTrapState.Lowered
            : nextWeight >= _halfRaisedWeight ? SpikeTrapState.HalfRaised : SpikeTrapState.Raised;
        if (target != TargetState) SpikeActivationGroup.Request(this, _changedButtons);
        SetState(target);
    }

    private static bool IsDestroyedOwner(MonoBehaviour owner) => owner == null;

    public SpikeTrapState CurrentState { get; private set; }
    public SpikeTrapState TargetState { get; private set; }
    public bool IsTransitioning => _phase != MotionPhase.Stable;
    public bool IsShaking => _phase == MotionPhase.Shaking;
    public bool IsMoving => _phase == MotionPhase.Moving;
    public bool IsPreparingActivation => isActiveAndEnabled &&
        (_phase == MotionPhase.Activating || _phase == MotionPhase.Shaking || _phase == MotionPhase.Ready);
    public Vector3 CurrentLocalPosition => ReadPhysicsLocalPosition();

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ConfigureRigidbody();
        _activationGlow = GetComponent<ActivationGlow>();
        if (_activationGlow == null) _activationGlow = gameObject.AddComponent<ActivationGlow>();
        if (_activationRenderers != null && _activationRenderers.Length > 0)
            _activationGlow.Configure(_activationRenderers);
        _visualShakeCenter = _visualShakeRoot.localPosition;
        SnapToState(_initialState);
        _initialized = true;
    }

    private void Update()
    {
        if (_paused) return;
        if (_phase == MotionPhase.Activating) { UpdateActivationEffects(); return; }
        if (_phase != MotionPhase.Shaking)
        {
            return;
        }

        UpdateShake();
    }

    private void FixedUpdate()
    {
        if (_paused || _grouped || _phase != MotionPhase.Moving)
        {
            return;
        }

        UpdateVerticalMovement();
    }

    public void SetState(SpikeTrapState targetState)
    {
        if (!_initialized || !isActiveAndEnabled || _motionRigidbody == null)
        {
            return;
        }

        if (_phase == MotionPhase.Stable && CurrentState == targetState)
        {
            TargetState = targetState;
            return;
        }

        if (_phase != MotionPhase.Stable && TargetState == targetState)
        {
            return;
        }

        TargetState = targetState;

        if (_phase == MotionPhase.Activating || _phase == MotionPhase.Shaking || _phase == MotionPhase.Ready)
        {
            if (TargetState == CurrentState &&
                (ReadPhysicsLocalPosition() - GetLocalPosition(CurrentState)).sqrMagnitude <= PositionEpsilonSquared)
            {
                CancelShakeAndRemainStable();
            }

            return;
        }

        if (_phase == MotionPhase.Moving)
        {
            BeginVerticalMovement();
            return;
        }

        BeginActivationEffects();
    }

    private void BeginActivationEffects()
    {
        _phase = MotionPhase.Activating;
        _phaseElapsed = 0f;
        _particlesStopped = false;
        _activationParticles?.Play(true);
        if (_paused) _activationParticles?.Pause(true);
        if (_glowDuration <= 0f && _activationParticles == null) FinishActivationEffects();
    }

    private void UpdateActivationEffects()
    {
        _phaseElapsed += Time.deltaTime;
        float t = _glowDuration > 0f ? Mathf.Clamp01(_phaseElapsed / _glowDuration) : 1f;
        SetActivationGlow(Mathf.Sin(t * Mathf.PI) * _glowIntensity);
        if (t < 1f) return;
        SetActivationGlow(0f);
        if (!_particlesStopped)
        {
            _particlesStopped = true;
            _activationParticles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (_activationParticles != null && _activationParticles.IsAlive(true)) return;
        FinishActivationEffects();
    }

    private void FinishActivationEffects()
    {
        if (RequiresWarningShake(CurrentState))
        {
            BeginShake();
        }
        else
        {
            BeginVerticalMovement();
        }
    }

    private void SetActivationGlow(float value) => _activationGlow?.SetIntensity(value);

    internal bool ActivationPaused => _paused;
    internal bool GroupReady => _phase == MotionPhase.Ready || _phase == MotionPhase.Moving;
    internal float MoveDuration => _moveDuration;
    internal void JoinActivationGroup()
    {
        _grouped = true;
        if (_phase == MotionPhase.Moving) _phase = MotionPhase.Ready;
    }
    internal void LeaveActivationGroup()
    {
        _grouped = false;
        if (_phase == MotionPhase.Ready) BeginVerticalMovement();
    }
    internal void BeginGroupMovement()
    {
        RecenterVisualShake();
        _moveStartLocalPosition = ReadPhysicsLocalPosition();
        _moveTargetLocalPosition = GetLocalPosition(TargetState);
        _phase = MotionPhase.Moving;
    }
    internal void TickGroupMovement(float normalizedTime) => ApplyVerticalMovement(normalizedTime);

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;
        if (_activationParticles == null) return;
        if (paused && _activationParticles.isPlaying) _activationParticles.Pause(true);
        else if (!paused && _activationParticles.isPaused) _activationParticles.Play(true);
    }

    private void BeginShake()
    {
        RecenterVisualShake();

        if (_shakeDuration <= Mathf.Epsilon ||
            _shakeAmplitude <= Mathf.Epsilon ||
            _shakeLocalDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            BeginVerticalMovement();
            return;
        }

        _phase = MotionPhase.Shaking;
        _phaseElapsed = 0f;
    }

    private void UpdateShake()
    {
        _phaseElapsed += Time.deltaTime;
        float normalizedTime = _shakeDuration <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(_phaseElapsed / _shakeDuration);

        float angle = _phaseElapsed * _shakeFrequency * Mathf.PI * 2f;
        float damping = 1f - normalizedTime;
        float offset = Mathf.Sin(angle) * _shakeAmplitude * damping;
        Vector3 direction = _shakeLocalDirection.normalized;

        _visualShakeRoot.localPosition = _visualShakeCenter + direction * offset;

        if (normalizedTime < 1f)
        {
            return;
        }

        RecenterVisualShake();

        if (TargetState == CurrentState)
        {
            _phase = MotionPhase.Stable;
            _phaseElapsed = 0f;
            return;
        }

        BeginVerticalMovement();
    }

    private void BeginVerticalMovement()
    {
        if (_grouped)
        {
            RecenterVisualShake();
            _phase = MotionPhase.Ready;
            return;
        }
        RecenterVisualShake();
        _moveStartLocalPosition = ReadPhysicsLocalPosition();
        _moveTargetLocalPosition = GetLocalPosition(TargetState);
        _phaseElapsed = 0f;
        _phase = MotionPhase.Moving;

        if (_moveDuration <= Mathf.Epsilon ||
            (_moveTargetLocalPosition - _moveStartLocalPosition).sqrMagnitude <= PositionEpsilonSquared)
        {
            SnapPhysicsToLocalPosition(_moveTargetLocalPosition);
            CompleteVerticalMovement();
        }
    }

    private void UpdateVerticalMovement()
    {
        _phaseElapsed += Time.fixedDeltaTime;
        float normalizedTime = _moveDuration <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(_phaseElapsed / _moveDuration);

        ApplyVerticalMovement(normalizedTime);
    }

    private void ApplyVerticalMovement(float normalizedTime)
    {
        float curvedTime = _moveCurve != null
            ? _moveCurve.Evaluate(normalizedTime)
            : normalizedTime;

        Vector3 nextLocalPosition = Vector3.LerpUnclamped(
            _moveStartLocalPosition,
            _moveTargetLocalPosition,
            curvedTime);

        MovePhysicsToLocalPosition(nextLocalPosition);

        if (normalizedTime < 1f)
        {
            return;
        }

        MovePhysicsToLocalPosition(_moveTargetLocalPosition);
        CompleteVerticalMovement();
    }

    private void CompleteVerticalMovement()
    {
        _grouped = false;
        CurrentState = TargetState;
        _phase = MotionPhase.Stable;
        _phaseElapsed = 0f;
    }

    private void CancelShakeAndRemainStable()
    {
        _grouped = false;
        SetActivationGlow(0f);
        _activationParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        RecenterVisualShake();
        TargetState = CurrentState;
        _phase = MotionPhase.Stable;
        _phaseElapsed = 0f;
    }

    private void SnapToState(SpikeTrapState state)
    {
        CurrentState = state;
        TargetState = state;
        _phase = MotionPhase.Stable;
        _phaseElapsed = 0f;
        RecenterVisualShake();
        SnapPhysicsToLocalPosition(GetLocalPosition(state));
    }

    private void ConfigureRigidbody()
    {
        _motionRigidbody.isKinematic = true;
        _motionRigidbody.useGravity = false;
        _motionRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _motionRigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    private bool ValidateReferences()
    {
        if (_motionRoot == null)
        {
            Debug.LogError($"{nameof(SpikeTrapController)} requiere Motion Root.", this);
            return false;
        }

        if (_motionRigidbody == null)
        {
            _motionRigidbody = _motionRoot.GetComponent<Rigidbody>();
        }

        if (_motionRigidbody == null)
        {
            Debug.LogError($"{nameof(SpikeTrapController)} requiere un Rigidbody en Motion Root.", this);
            return false;
        }

        if (_visualShakeRoot == null)
        {
            Debug.LogError($"{nameof(SpikeTrapController)} requiere Visual Shake Root.", this);
            return false;
        }

        if (_motionRigidbody.transform != _motionRoot || _visualShakeRoot == _motionRoot ||
            !_visualShakeRoot.IsChildOf(_motionRoot))
        {
            Debug.LogError("Motion Rigidbody debe pertenecer a Motion Root y Visual Shake Root debe ser un hijo separado.", this);
            return false;
        }

        return true;
    }

    private Vector3 ReadPhysicsLocalPosition()
    {
        if (_motionRigidbody == null || _motionRoot == null)
        {
            return Vector3.zero;
        }

        Transform parent = _motionRoot.parent;
        return parent != null
            ? parent.InverseTransformPoint(_motionRigidbody.position)
            : _motionRigidbody.position;
    }

    private void MovePhysicsToLocalPosition(Vector3 localPosition)
    {
        Transform parent = _motionRoot.parent;
        Vector3 worldPosition = parent != null
            ? parent.TransformPoint(localPosition)
            : localPosition;

        _motionRigidbody.MovePosition(worldPosition);
    }

    private void SnapPhysicsToLocalPosition(Vector3 localPosition)
    {
        Transform parent = _motionRoot.parent;
        Vector3 worldPosition = parent != null
            ? parent.TransformPoint(localPosition)
            : localPosition;

        _motionRigidbody.position = worldPosition;
        _motionRoot.localPosition = localPosition;
    }

    private void RecenterVisualShake()
    {
        if (_visualShakeRoot != null)
        {
            _visualShakeRoot.localPosition = _visualShakeCenter;
        }
    }

    private Vector3 GetLocalPosition(SpikeTrapState state)
    {
        return state switch
        {
            SpikeTrapState.HalfRaised => _halfRaisedLocalPosition,
            SpikeTrapState.Lowered => _loweredLocalPosition,
            _ => _raisedLocalPosition
        };
    }

    private static bool RequiresWarningShake(SpikeTrapState originState)
    {
        return originState == SpikeTrapState.Raised ||
               originState == SpikeTrapState.HalfRaised;
    }

    private void OnDisable()
    {
        _grouped = false;
        RecenterVisualShake();
        SetActivationGlow(0f);
        _activationParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_phase == MotionPhase.Activating || _phase == MotionPhase.Shaking || _phase == MotionPhase.Ready)
            CancelShakeAndRemainStable();
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
    }

    private void OnDestroy()
    {
        foreach (var button in _buttonConnections.Keys)
            if (button != null) button.UnregisterActivationTrap(this);
    }

    private void OnValidate()
    {
        _halfRaisedWeight = Mathf.Clamp(_halfRaisedWeight, 1, int.MaxValue - 1);
        _loweredWeight = Mathf.Max(_halfRaisedWeight + 1, _loweredWeight);
        _refreshWeight = true;
        _moveDuration = Mathf.Max(0f, _moveDuration);
        _shakeAmplitude = Mathf.Max(0f, _shakeAmplitude);
        _shakeFrequency = Mathf.Max(0f, _shakeFrequency);
        _shakeDuration = Mathf.Max(0f, _shakeDuration);
        _glowDuration = Mathf.Max(0f, _glowDuration);
        _glowIntensity = Mathf.Max(0f, _glowIntensity);

        if (_motionRoot != null && _motionRigidbody == null)
        {
            _motionRigidbody = _motionRoot.GetComponent<Rigidbody>();
        }
    }

    [ContextMenu("Capture Motion Root Position As Raised")]
    private void CaptureRaisedPosition()
    {
        if (_motionRoot != null)
        {
            _raisedLocalPosition = _motionRoot.localPosition;
        }
    }

    [ContextMenu("Capture Motion Root Position As Half Raised")]
    private void CaptureHalfRaisedPosition()
    {
        if (_motionRoot != null)
        {
            _halfRaisedLocalPosition = _motionRoot.localPosition;
        }
    }

    [ContextMenu("Capture Motion Root Position As Lowered")]
    private void CaptureLoweredPosition()
    {
        if (_motionRoot != null)
        {
            _loweredLocalPosition = _motionRoot.localPosition;
        }
    }
}
