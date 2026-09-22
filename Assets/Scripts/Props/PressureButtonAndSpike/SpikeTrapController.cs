using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla una trampa de lanzas mediante una única secuencia redirigible: ejecuta una advertencia
/// visual cuando corresponde y mueve el cuerpo cinemático vertical junto con su collider en FixedUpdate.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpikeTrapController : MonoBehaviour, ISpikeTrapController
{
    private enum MotionPhase
    {
        Stable,
        Shaking,
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
        _weightDriven = true;
        _refreshWeight = true;
    }

    /// <summary>Retira sólo las conexiones creadas por este coordinador u orquestador.</summary>
    public void UnregisterButtons(MonoBehaviour connectionOwner)
    {
        foreach (HashSet<MonoBehaviour> owners in _buttonConnections.Values)
            owners.Remove(connectionOwner);
        _refreshWeight = true;
    }

    private void OnEnable() => _refreshWeight = true;

    private void LateUpdate()
    {
        if (!_initialized || !_weightDriven) return;
        long total = 0;
        _removedButtons.Clear();
        _contributingButtons.Clear();
        foreach (var pair in _buttonConnections)
        {
            pair.Value.RemoveWhere(IsDestroyedOwner);
            if (pair.Key == null || pair.Value.Count == 0)
            {
                _removedButtons.Add(pair.Key);
                continue;
            }

            if (!pair.Key.isActiveAndEnabled) continue;
            bool hasActiveConnection = false;
            foreach (MonoBehaviour owner in pair.Value)
                if (owner.isActiveAndEnabled) { hasActiveConnection = true; break; }
            if (!hasActiveConnection) continue;

            _contributingButtons.Add(pair.Key);
            total += pair.Key.EffectiveWeight;
        }
        foreach (PressureButtonStateResolver button in _removedButtons) _buttonConnections.Remove(button);

        int nextWeight = (int)System.Math.Min(int.MaxValue, total);
        if (!_refreshWeight && nextWeight == TotalEffectiveWeight) return;
        _refreshWeight = false;
        TotalEffectiveWeight = nextWeight;
        // Una única orden por frame, después de los cambios de sensores y temporizadores.
        SetState(nextWeight >= _loweredWeight ? SpikeTrapState.Lowered
            : nextWeight >= _halfRaisedWeight ? SpikeTrapState.HalfRaised : SpikeTrapState.Raised);
    }

    private static bool IsDestroyedOwner(MonoBehaviour owner) => owner == null;

    public SpikeTrapState CurrentState { get; private set; }
    public SpikeTrapState TargetState { get; private set; }
    public bool IsTransitioning => _phase != MotionPhase.Stable;
    public bool IsShaking => _phase == MotionPhase.Shaking;
    public bool IsMoving => _phase == MotionPhase.Moving;
    public Vector3 CurrentLocalPosition => ReadPhysicsLocalPosition();

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ConfigureRigidbody();
        _visualShakeCenter = _visualShakeRoot.localPosition;
        SnapToState(_initialState);
        _initialized = true;
    }

    private void Update()
    {
        if (_phase != MotionPhase.Shaking)
        {
            return;
        }

        UpdateShake();
    }

    private void FixedUpdate()
    {
        if (_phase != MotionPhase.Moving)
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

        if (_phase == MotionPhase.Shaking)
        {
            if (TargetState == CurrentState)
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

        if (RequiresWarningShake(CurrentState))
        {
            BeginShake();
        }
        else
        {
            BeginVerticalMovement();
        }
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
        CurrentState = TargetState;
        _phase = MotionPhase.Stable;
        _phaseElapsed = 0f;
    }

    private void CancelShakeAndRemainStable()
    {
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
        RecenterVisualShake();
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
