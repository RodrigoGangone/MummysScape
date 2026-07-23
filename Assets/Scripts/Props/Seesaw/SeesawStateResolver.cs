using System;
using UnityEngine;

/// <summary>
/// Convierte los pesos reales de ambos extremos en pesos efectivos, estado y nivel de velocidad.
/// Aplica únicamente umbrales inferiores y conserva los valores completos sin controlar movimiento.
/// </summary>
[DisallowMultipleComponent]
public sealed class SeesawStateResolver : MonoBehaviour
{
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

    private SeesawResolution _currentResolution;

    public int MinimumActivationWeight => _minimumActivationWeight;
    public int MinimumWeightDifference => _minimumWeightDifference;
    public int LeftRawWeight => _leftRawWeight;
    public int RightRawWeight => _rightRawWeight;
    public int LeftEffectiveWeight => _leftEffectiveWeight;
    public int RightEffectiveWeight => _rightEffectiveWeight;
    public int WeightDifference => _weightDifference;
    public SeesawState CurrentState => _currentState;
    public SeesawSpeedLevel CurrentSpeedLevel => _currentSpeedLevel;
    public SeesawResolution CurrentResolution => _currentResolution;

    public event Action<SeesawState> StateChanged;
    public event Action<SeesawSpeedLevel> SpeedLevelChanged;
    public event Action<SeesawResolution> ResolutionChanged;

    public SeesawResolution Resolve(int leftRawWeight, int rightRawWeight)
    {
        int safeLeftRawWeight = Mathf.Max(0, leftRawWeight);
        int safeRightRawWeight = Mathf.Max(0, rightRawWeight);

        int leftEffectiveWeight = safeLeftRawWeight >= _minimumActivationWeight
            ? safeLeftRawWeight
            : 0;

        int rightEffectiveWeight = safeRightRawWeight >= _minimumActivationWeight
            ? safeRightRawWeight
            : 0;

        long differenceLong = Math.Abs((long)leftEffectiveWeight - rightEffectiveWeight);
        int difference = differenceLong > int.MaxValue
            ? int.MaxValue
            : (int)differenceLong;

        SeesawState state = ResolveState(
            leftEffectiveWeight,
            rightEffectiveWeight,
            difference);

        SeesawSpeedLevel speedLevel = ResolveSpeedLevel(state, difference);

        SeesawResolution nextResolution = new SeesawResolution(
            safeLeftRawWeight,
            safeRightRawWeight,
            leftEffectiveWeight,
            rightEffectiveWeight,
            difference,
            state,
            speedLevel);

        ApplyResolution(nextResolution);
        return nextResolution;
    }

    private SeesawState ResolveState(
        int leftEffectiveWeight,
        int rightEffectiveWeight,
        int difference)
    {
        if (leftEffectiveWeight == rightEffectiveWeight ||
            difference < _minimumWeightDifference)
        {
            return SeesawState.Balanced;
        }

        return leftEffectiveWeight > rightEffectiveWeight
            ? SeesawState.LeftHeavy
            : SeesawState.RightHeavy;
    }

    private SeesawSpeedLevel ResolveSpeedLevel(SeesawState state, int difference)
    {
        if (state == SeesawState.Balanced)
        {
            return SeesawSpeedLevel.None;
        }

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