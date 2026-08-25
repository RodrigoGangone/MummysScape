using System;
using UnityEngine;

/// <summary>
/// Proporciona una base común para componentes de peso y centraliza la notificación de cambios
/// de valor o disponibilidad sin acoplar al botón con implementaciones concretas.
/// </summary>
public abstract class WeightProviderBehaviour : MonoBehaviour, IWeightProvider
{
    public abstract int Weight { get; }
    public event Action WeightChanged;

    protected void NotifyWeightChanged()
    {
        WeightChanged?.Invoke();
    }

    protected virtual void OnEnable()
    {
        NotifyWeightChanged();
    }

    protected virtual void OnDisable()
    {
        NotifyWeightChanged();
    }
}
