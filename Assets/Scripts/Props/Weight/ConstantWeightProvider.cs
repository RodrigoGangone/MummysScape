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

    /// <summary>Actualiza el peso y notifica a sensores que ya estén detectando este objeto.</summary>
    public void SetWeight(int weight)
    {
        int next = Mathf.Max(0, weight);
        if (_weight == next) return;
        _weight = next;
        NotifyWeightChanged();
    }

    private void OnValidate()
    {
        _weight = Mathf.Max(0, _weight);

        if (Application.isPlaying)
        {
            NotifyWeightChanged();
        }
    }
}
