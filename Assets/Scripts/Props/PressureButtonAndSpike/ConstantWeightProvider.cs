using UnityEngine;

/// <summary>
/// Expone un peso constante configurable para cajas, vendas y cualquier objeto reutilizable
/// cuyo peso no dependa de un estado dinámico.
/// </summary>
[DisallowMultipleComponent]
public sealed class ConstantWeightProvider : WeightProviderBehaviour
{
    [SerializeField, Min(0)] private int _weight = 1;

    public override int Weight => Mathf.Max(0, _weight);

    private void OnValidate()
    {
        int clampedWeight = Mathf.Max(0, _weight);
        if (clampedWeight == _weight)
        {
            return;
        }

        _weight = clampedWeight;

        if (Application.isPlaying)
        {
            NotifyWeightChanged();
        }
    }
}
