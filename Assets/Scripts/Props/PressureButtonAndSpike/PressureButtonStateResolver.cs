using System;
using UnityEngine;

/// <summary>
/// Convierte el peso real en el estado efectivo del botón y aplica la regla de retención temporal
/// sin conocer detalles visuales, trampas ni detección física interna.
/// </summary>
[DisallowMultipleComponent]
public sealed class PressureButtonStateResolver : MonoBehaviour
{
    private const int FullPressThreshold = 2;

    [SerializeField] private PressureButtonWeightSensor _weightSensor;
    [SerializeField] private PressureButtonHoldTimer _holdTimer;

    private bool _fullyPressedWasReached;

    public PressureButtonState EffectiveState { get; private set; } = PressureButtonState.Released;
    public int RealWeight => _weightSensor != null ? _weightSensor.TotalWeight : 0;

    public event Action<PressureButtonState> EffectiveStateChanged;

    private void OnEnable()
    {
        if (_weightSensor != null)
        {
            _weightSensor.TotalWeightChanged += HandleWeightChanged;
        }

        if (_holdTimer != null)
        {
            _holdTimer.Completed += HandleHoldCompleted;
        }

        EvaluateWeight(RealWeight);
    }

    private void OnDisable()
    {
        if (_weightSensor != null)
        {
            _weightSensor.TotalWeightChanged -= HandleWeightChanged;
        }

        if (_holdTimer != null)
        {
            _holdTimer.Completed -= HandleHoldCompleted;
            _holdTimer.Cancel();
        }
    }

    private void HandleWeightChanged(int totalWeight)
    {
        EvaluateWeight(totalWeight);
    }

    private void EvaluateWeight(int totalWeight)
    {
        totalWeight = Mathf.Max(0, totalWeight);

        if (totalWeight >= FullPressThreshold)
        {
            _fullyPressedWasReached = true;

            if (_holdTimer != null && _holdTimer.IsRunning)
            {
                _holdTimer.Cancel();
            }

            SetEffectiveState(PressureButtonState.FullyPressed);
            return;
        }

        if (EffectiveState == PressureButtonState.FullyPressed && _fullyPressedWasReached)
        {
            if (_holdTimer == null)
            {
                Debug.LogError($"{nameof(PressureButtonStateResolver)} requiere un {nameof(PressureButtonHoldTimer)}.", this);
                _fullyPressedWasReached = false;
                SetEffectiveState(MapWeightToState(totalWeight));
                return;
            }

            if (_holdTimer.IsRunning || _holdTimer.StartTimer())
            {
                return;
            }

            _fullyPressedWasReached = false;
        }

        SetEffectiveState(MapWeightToState(totalWeight));
    }

    private void HandleHoldCompleted()
    {
        int currentWeight = RealWeight;

        if (currentWeight >= FullPressThreshold)
        {
            _fullyPressedWasReached = true;
            SetEffectiveState(PressureButtonState.FullyPressed);
            _holdTimer.ResetProgress();
            return;
        }

        _fullyPressedWasReached = false;
        SetEffectiveState(MapWeightToState(currentWeight));
        _holdTimer.ResetProgress();
    }

    private void SetEffectiveState(PressureButtonState newState)
    {
        if (EffectiveState == newState)
        {
            return;
        }

        EffectiveState = newState;
        EffectiveStateChanged?.Invoke(EffectiveState);
    }

    private static PressureButtonState MapWeightToState(int weight)
    {
        if (weight <= 0)
        {
            return PressureButtonState.Released;
        }

        return weight == 1
            ? PressureButtonState.HalfPressed
            : PressureButtonState.FullyPressed;
    }

    private void OnValidate()
    {
        if (_weightSensor == null)
        {
            _weightSensor = GetComponentInChildren<PressureButtonWeightSensor>(true);
        }

        if (_holdTimer == null)
        {
            _holdTimer = GetComponent<PressureButtonHoldTimer>();
        }
    }
}
