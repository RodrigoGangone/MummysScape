using System;

/// <summary>
/// Define el estado lógico del balancín según qué lado posee mayor peso efectivo.
/// </summary>
public enum SeesawState
{
    Balanced,
    LeftHeavy,
    RightHeavy
}

/// <summary>
/// Define el nivel discreto de velocidad solicitado a partir de la diferencia de peso.
/// </summary>
public enum SeesawSpeedLevel
{
    None,
    Low,
    Medium,
    High
}

/// <summary>
/// Contiene el resultado inmutable de resolver los pesos reales del balancín, incluyendo
/// pesos efectivos, diferencia, estado y nivel de velocidad sin controlar la física.
/// </summary>
public readonly struct SeesawResolution : IEquatable<SeesawResolution>
{
    public SeesawResolution(
        int leftRawWeight,
        int rightRawWeight,
        int leftEffectiveWeight,
        int rightEffectiveWeight,
        int weightDifference,
        SeesawState state,
        SeesawSpeedLevel speedLevel)
    {
        LeftRawWeight = leftRawWeight;
        RightRawWeight = rightRawWeight;
        LeftEffectiveWeight = leftEffectiveWeight;
        RightEffectiveWeight = rightEffectiveWeight;
        WeightDifference = weightDifference;
        State = state;
        SpeedLevel = speedLevel;
    }

    public int LeftRawWeight { get; }
    public int RightRawWeight { get; }
    public int LeftEffectiveWeight { get; }
    public int RightEffectiveWeight { get; }
    public int WeightDifference { get; }
    public SeesawState State { get; }
    public SeesawSpeedLevel SpeedLevel { get; }

    public bool Equals(SeesawResolution other)
    {
        return LeftRawWeight == other.LeftRawWeight &&
               RightRawWeight == other.RightRawWeight &&
               LeftEffectiveWeight == other.LeftEffectiveWeight &&
               RightEffectiveWeight == other.RightEffectiveWeight &&
               WeightDifference == other.WeightDifference &&
               State == other.State &&
               SpeedLevel == other.SpeedLevel;
    }

    public override bool Equals(object obj)
    {
        return obj is SeesawResolution other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            LeftRawWeight,
            RightRawWeight,
            LeftEffectiveWeight,
            RightEffectiveWeight,
            WeightDifference,
            State,
            SpeedLevel);
    }

    public static bool operator ==(SeesawResolution left, SeesawResolution right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SeesawResolution left, SeesawResolution right)
    {
        return !left.Equals(right);
    }
}