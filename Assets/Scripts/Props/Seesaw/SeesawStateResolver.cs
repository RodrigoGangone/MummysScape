using System;
using UnityEngine;

/// <summary>
/// Convierte los pesos de ambos extremos en el estado objetivo del sube y baja.
/// Equilibra la tabla cuando existen pesos activos iguales, conserva la última inclinación cuando queda vacía
/// y cambia de lado únicamente cuando una diferencia válida determina un nuevo extremo dominante.
/// </summary>
[DisallowMultipleComponent]
public sealed class SeesawStateResolver : MonoBehaviour
{
    [Header("Initial State")]
    [SerializeField] private SeesawInitialPosition _initialPosition = SeesawInitialPosition.Middle;

    [Header("Activation")]
    [SerializeField, Min(0)] private int _minimumActivationWeight = 1;
    [SerializeField, Min(0)] private int _minimumWeightDifference = 1;

    [Header("Speed Thresholds")]
    [SerializeField, Min(1)] private int _mediumSpeedThreshold = 3;
    [SerializeField, Min(2)] private int _highSpeedThreshold = 6;

    [Header("Runtime Debug")]
    [SerializeField] private int _leftRawWeight;
    [SerializeField] private int _rightRawWeight;
    [SerializeField] private int _leftEffectiveWeight;
    [SerializeField] private int _rightEffectiveWeight;
    [SerializeField] private int _weightDifference;
    [SerializeField] private SeesawState _currentState = SeesawState.Balanced;
    [SerializeField] private SeesawSpeedLevel _currentSpeedLevel = SeesawSpeedLevel.None;
    [SerializeField] private SeesawState _lastStableState = SeesawState.Balanced;
    [SerializeField] private SeesawSpeedLevel _lastStableSpeedLevel = SeesawSpeedLevel.None;

    private SeesawResolution _currentResolution;
    private bool _isInitialized;

    public SeesawInitialPosition InitialPosition => _initialPosition;
    public int MinimumActivationWeight => _minimumActivationWeight;
    public int MinimumWeightDifference => _minimumWeightDifference;
    public int LeftRawWeight => _leftRawWeight;
    public int RightRawWeight => _rightRawWeight;
    public int LeftEffectiveWeight => _leftEffectiveWeight;
    public int RightEffectiveWeight => _rightEffectiveWeight;
    public int WeightDifference => _weightDifference;
    public SeesawState CurrentState => _currentState;
    public SeesawState LastStableState => _lastStableState;
    public SeesawSpeedLevel CurrentSpeedLevel => _currentSpeedLevel;
    public SeesawResolution CurrentResolution => _currentResolution;

    public event Action<SeesawState> StateChanged;
    public event Action<SeesawSpeedLevel> SpeedLevelChanged;
    public event Action<SeesawResolution> ResolutionChanged;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _lastStableState = ConvertInitialPositionToState(_initialPosition);
        _lastStableSpeedLevel = SeesawSpeedLevel.None;

        SeesawResolution initialResolution = new SeesawResolution(
            0,
            0,
            0,
            0,
            0,
            _lastStableState,
            SeesawSpeedLevel.None);

        _isInitialized = true;
        ApplyResolution(initialResolution);
    }

    public SeesawResolution Resolve(int leftRawWeight, int rightRawWeight)
    {
        Initialize();

        int safeLeftRawWeight = Mathf.Max(0, leftRawWeight);
        int safeRightRawWeight = Mathf.Max(0, rightRawWeight);

        int leftEffectiveWeight = ResolveEffectiveWeight(safeLeftRawWeight);
        int rightEffectiveWeight = ResolveEffectiveWeight(safeRightRawWeight);
        int difference = CalculateSafeDifference(leftEffectiveWeight, rightEffectiveWeight);

        ResolveTarget(
            leftEffectiveWeight,
            rightEffectiveWeight,
            difference,
            out SeesawState targetState,
            out SeesawSpeedLevel targetSpeedLevel);

        SeesawResolution nextResolution = new SeesawResolution(
            safeLeftRawWeight,
            safeRightRawWeight,
            leftEffectiveWeight,
            rightEffectiveWeight,
            difference,
            targetState,
            targetSpeedLevel);

        ApplyResolution(nextResolution);
        return nextResolution;
    }

    private int ResolveEffectiveWeight(int rawWeight)
    {
        return rawWeight >= _minimumActivationWeight
            ? rawWeight
            : 0;
    }

    private void ResolveTarget(
        int leftEffectiveWeight,
        int rightEffectiveWeight,
        int difference,
        out SeesawState targetState,
        out SeesawSpeedLevel targetSpeedLevel)
    {
        bool bothSidesAreEmpty = leftEffectiveWeight == 0 && rightEffectiveWeight == 0;

        if (bothSidesAreEmpty)
        {
            ResolveLastStableTarget(out targetState, out targetSpeedLevel);
            return;
        }

        bool hasEqualActiveWeight =
            leftEffectiveWeight > 0 &&
            leftEffectiveWeight == rightEffectiveWeight;

        if (hasEqualActiveWeight)
        {
            targetState = SeesawState.Balanced;
            targetSpeedLevel = SeesawSpeedLevel.None;
            return;
        }

        if (difference < _minimumWeightDifference)
        {
            ResolveLastStableTarget(out targetState, out targetSpeedLevel);
            return;
        }

        targetState = leftEffectiveWeight > rightEffectiveWeight
            ? SeesawState.LeftHeavy
            : SeesawState.RightHeavy;

        targetSpeedLevel = ResolveSpeedLevel(difference);

        _lastStableState = targetState;
        _lastStableSpeedLevel = targetSpeedLevel;
    }

    private void ResolveLastStableTarget(
        out SeesawState targetState,
        out SeesawSpeedLevel targetSpeedLevel)
    {
        targetState = _lastStableState;
        targetSpeedLevel = _lastStableState == SeesawState.Balanced
            ? SeesawSpeedLevel.None
            : _lastStableSpeedLevel;
    }

    private static int CalculateSafeDifference(int leftWeight, int rightWeight)
    {
        long difference = Math.Abs((long)leftWeight - rightWeight);
        return difference > int.MaxValue
            ? int.MaxValue
            : (int)difference;
    }

    private SeesawSpeedLevel ResolveSpeedLevel(int difference)
    {
        if (difference >= _highSpeedThreshold)
        {
            return SeesawSpeedLevel.High;
        }

        if (difference >= _mediumSpeedThreshold)
        {
            return SeesawSpeedLevel.Medium;
        }

        return SeesawSpeedLevel.Low;
    }

    private static SeesawState ConvertInitialPositionToState(SeesawInitialPosition initialPosition)
    {
        return initialPosition switch
        {
            SeesawInitialPosition.Left => SeesawState.LeftHeavy,
            SeesawInitialPosition.Right => SeesawState.RightHeavy,
            _ => SeesawState.Balanced
        };
    }

    private void ApplyResolution(SeesawResolution nextResolution)
    {
        SeesawState previousState = _currentState;
        SeesawSpeedLevel previousSpeedLevel = _currentSpeedLevel;
        bool resolutionChanged = nextResolution != _currentResolution;

        _currentResolution = nextResolution;
        _leftRawWeight = nextResolution.LeftRawWeight;
        _rightRawWeight = nextResolution.RightRawWeight;
        _leftEffectiveWeight = nextResolution.LeftEffectiveWeight;
        _rightEffectiveWeight = nextResolution.RightEffectiveWeight;
        _weightDifference = nextResolution.WeightDifference;
        _currentState = nextResolution.State;
        _currentSpeedLevel = nextResolution.SpeedLevel;

        if (previousState != _currentState)
        {
            StateChanged?.Invoke(_currentState);
        }

        if (previousSpeedLevel != _currentSpeedLevel)
        {
            SpeedLevelChanged?.Invoke(_currentSpeedLevel);
        }

        if (resolutionChanged)
        {
            ResolutionChanged?.Invoke(_currentResolution);
        }
    }

    private void OnValidate()
    {
        _minimumActivationWeight = Mathf.Max(0, _minimumActivationWeight);
        _minimumWeightDifference = Mathf.Max(0, _minimumWeightDifference);

        int minimumMediumThreshold = Mathf.Max(1, _minimumWeightDifference);
        _mediumSpeedThreshold = Mathf.Max(minimumMediumThreshold, _mediumSpeedThreshold);
        _highSpeedThreshold = Mathf.Max(_mediumSpeedThreshold + 1, _highSpeedThreshold);
    }
}