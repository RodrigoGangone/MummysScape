using UnityEngine;

public interface IImpactSource
{
    KnockbackData GetKnockbackData(Vector3 victimPosition);
}

public struct KnockbackData
{
    public Vector3 TargetPosition;
    public float Duration;
}