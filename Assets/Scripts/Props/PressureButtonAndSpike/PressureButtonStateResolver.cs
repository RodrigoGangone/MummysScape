using System;
using UnityEngine;

/// <summary>Resuelve la placa y el aporte de peso, con retención independiente por botón.</summary>
[DisallowMultipleComponent]
public sealed class PressureButtonStateResolver : MonoBehaviour
{
    [Header("Umbrales del botón")]
    [SerializeField, Min(1), Tooltip("Peso mínimo para presionar la placa a la mitad.")]
    private int _halfPressThreshold = 1;
    [SerializeField, Min(2), Tooltip("Peso mínimo para pulsación completa y para armar la retención.")]
    private int _fullPressThreshold = 2;

    [Header("Referencias internas")]
    [SerializeField] private WeightSensor _weightSensor;
    [SerializeField] private PressureButtonHoldTimer _holdTimer;

    private bool _fullyPressedWasReached;
    private bool _configurationChanged;

    public PressureButtonState EffectiveState { get; private set; } = PressureButtonState.Released;
    public int RealWeight => _weightSensor != null && _weightSensor.isActiveAndEnabled
        ? _weightSensor.TotalWeight : 0;
    public int EffectiveWeight { get; private set; }
    public event Action<PressureButtonState> EffectiveStateChanged;
    public event Action<int> EffectiveWeightChanged;

    private void Awake() => ResolveReferences();

    private void OnEnable()
    {
        ResolveReferences();
        if (_weightSensor != null)
            _weightSensor.TotalWeightChanged += EvaluateWeight;
        else
            Debug.LogError("El botón requiere un WeightSensor en su jerarquía.", this);

        if (_holdTimer != null)
        {
            _holdTimer.Completed += HandleHoldCompleted;
            _holdTimer.Cancelled += HandleHoldCancelled;
        }
        EvaluateWeight(RealWeight);
    }

    // Sincroniza después de que todos los componentes hayan ejecutado Awake/OnEnable.
    private void Start() => EvaluateWeight(RealWeight);

    private void Update()
    {
        if (!_configurationChanged) return;
        _configurationChanged = false;
        EvaluateWeight(RealWeight);
    }

    private void OnDisable()
    {
        if (_weightSensor != null)
            _weightSensor.TotalWeightChanged -= EvaluateWeight;
        if (_holdTimer != null)
        {
            _holdTimer.Completed -= HandleHoldCompleted;
            _holdTimer.Cancelled -= HandleHoldCancelled;
            _holdTimer.Cancel();
        }
        _fullyPressedWasReached = false;
        PublishWeight(0);
    }

    private void EvaluateWeight(int totalWeight)
    {
        if (!isActiveAndEnabled) return;
        totalWeight = Mathf.Max(0, totalWeight);
        if (totalWeight >= _fullPressThreshold)
        {
            if (_holdTimer != null && _holdTimer.IsRunning) _holdTimer.Cancel();
            _fullyPressedWasReached = true;
            PublishWeight(totalWeight);
            return;
        }

        if (_fullyPressedWasReached && _holdTimer != null && _holdTimer.isActiveAndEnabled &&
            (_holdTimer.IsRunning || _holdTimer.StartTimer()))
        {
            PublishWeight(Mathf.Max(totalWeight, _fullPressThreshold));
            return;
        }

        _fullyPressedWasReached = false;
        PublishWeight(totalWeight);
    }

    private void HandleHoldCompleted()
    {
        _fullyPressedWasReached = false;
        EvaluateWeight(RealWeight);
        _holdTimer.ResetProgress();
    }

    private void HandleHoldCancelled()
    {
        _fullyPressedWasReached = false;
        PublishWeight(isActiveAndEnabled ? RealWeight : 0);
    }

    private void PublishWeight(int weight)
    {
        PressureButtonState state = weight >= _fullPressThreshold ? PressureButtonState.FullyPressed
            : weight >= _halfPressThreshold ? PressureButtonState.HalfPressed : PressureButtonState.Released;
        bool weightChanged = EffectiveWeight != weight;
        bool stateChanged = EffectiveState != state;
        // Ambos valores son coherentes antes de notificar a las vistas o a otros consumidores.
        EffectiveWeight = weight;
        EffectiveState = state;
        if (weightChanged) EffectiveWeightChanged?.Invoke(weight);
        if (stateChanged) EffectiveStateChanged?.Invoke(state);
    }

    private void ResolveReferences()
    {
        if (_weightSensor == null) _weightSensor = GetComponentInChildren<WeightSensor>(true);
        if (_holdTimer == null) _holdTimer = GetComponent<PressureButtonHoldTimer>();
    }

    private void OnValidate()
    {
        _halfPressThreshold = Mathf.Clamp(_halfPressThreshold, 1, int.MaxValue - 1);
        _fullPressThreshold = Mathf.Max(_halfPressThreshold + 1, _fullPressThreshold);
        // Las referencias se editan antes de Play; evita cambiar suscripciones desde OnValidate.
        if (!Application.isPlaying) ResolveReferences();
        _configurationChanged = true;
    }
}
