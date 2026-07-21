using UnityEngine;

/// <summary>
/// Mueve únicamente la placa visual entre tres posiciones locales configurables, permitiendo
/// redirigir el destino desde la posición actual sin superponer coroutines ni animaciones.
/// </summary>
[DisallowMultipleComponent]
public sealed class PressureButtonPlateMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _plate;

    [Header("Local Positions")]
    [SerializeField] private Vector3 _releasedLocalPosition;
    [SerializeField] private Vector3 _halfPressedLocalPosition;
    [SerializeField] private Vector3 _fullyPressedLocalPosition;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float _duration = 0.2f;
    [SerializeField] private AnimationCurve _movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private PressureButtonState _initialState = PressureButtonState.Released;

    private Vector3 _startLocalPosition;
    private Vector3 _targetLocalPosition;
    private float _elapsed;

    public PressureButtonState CurrentState { get; private set; }
    public PressureButtonState TargetState { get; private set; }
    public bool IsMoving { get; private set; }

    private void Awake()
    {
        if (_plate == null)
        {
            Debug.LogError($"{nameof(PressureButtonPlateMover)} no tiene asignada la placa visual.", this);
            enabled = false;
            return;
        }

        SnapToState(_initialState);
    }

    private void Update()
    {
        if (!IsMoving)
        {
            return;
        }

        if (_duration <= Mathf.Epsilon)
        {
            CompleteMovement();
            return;
        }

        _elapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(_elapsed / _duration);
        float curvedTime = _movementCurve != null
            ? _movementCurve.Evaluate(normalizedTime)
            : normalizedTime;

        _plate.localPosition = Vector3.LerpUnclamped(_startLocalPosition, _targetLocalPosition, curvedTime);

        if (normalizedTime >= 1f)
        {
            CompleteMovement();
        }
    }

    public void SetState(PressureButtonState targetState)
    {
        if (_plate == null)
        {
            return;
        }

        if (IsMoving && TargetState == targetState)
        {
            return;
        }

        if (!IsMoving && CurrentState == targetState)
        {
            TargetState = targetState;
            return;
        }

        TargetState = targetState;
        _startLocalPosition = _plate.localPosition;
        _targetLocalPosition = GetLocalPosition(targetState);
        _elapsed = 0f;
        IsMoving = true;

        if (_duration <= Mathf.Epsilon ||
            (_targetLocalPosition - _startLocalPosition).sqrMagnitude <= 0.0000001f)
        {
            CompleteMovement();
        }
    }

    private void CompleteMovement()
    {
        _plate.localPosition = _targetLocalPosition;
        CurrentState = TargetState;
        IsMoving = false;
        _elapsed = 0f;
    }

    private void SnapToState(PressureButtonState state)
    {
        CurrentState = state;
        TargetState = state;
        _targetLocalPosition = GetLocalPosition(state);
        _startLocalPosition = _targetLocalPosition;
        _plate.localPosition = _targetLocalPosition;
        _elapsed = 0f;
        IsMoving = false;
    }

    private Vector3 GetLocalPosition(PressureButtonState state)
    {
        return state switch
        {
            PressureButtonState.HalfPressed => _halfPressedLocalPosition,
            PressureButtonState.FullyPressed => _fullyPressedLocalPosition,
            _ => _releasedLocalPosition
        };
    }

    private void OnValidate()
    {
        _duration = Mathf.Max(0f, _duration);
    }

    [ContextMenu("Capture Plate Position As Released")]
    private void CaptureReleasedPosition()
    {
        if (_plate != null)
        {
            _releasedLocalPosition = _plate.localPosition;
        }
    }

    [ContextMenu("Capture Plate Position As Half Pressed")]
    private void CaptureHalfPressedPosition()
    {
        if (_plate != null)
        {
            _halfPressedLocalPosition = _plate.localPosition;
        }
    }

    [ContextMenu("Capture Plate Position As Fully Pressed")]
    private void CaptureFullyPressedPosition()
    {
        if (_plate != null)
        {
            _fullyPressedLocalPosition = _plate.localPosition;
        }
    }
}
