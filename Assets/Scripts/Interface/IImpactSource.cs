using UnityEngine;

/// <summary> 
/// Contrato de Impacto: Define cómo los objetos del entorno (trampas, proyectiles) entregan 
/// datos de retroceso (Knockback) calculados en base a la posición de la víctima. 
/// </summary>

public interface IImpactSource
{
    KnockbackData GetKnockbackData(Vector3 victimPosition);
}

public struct KnockbackData
{
    public Vector3 TargetPosition;
    public float Duration;
}