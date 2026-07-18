using System;

/// <summary>
/// Expone un peso no negativo y notifica cuando su valor efectivo puede haber cambiado.
/// </summary>
public interface IWeightProvider
{
    int Weight { get; }
    event Action WeightChanged;
}
