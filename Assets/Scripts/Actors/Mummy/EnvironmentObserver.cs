using UnityEngine;

/// <summary> 
/// Observador de Impactos: Detecta colisiones o triggers provenientes de fuentes externas y extrae 
/// los datos de retroceso (Knockback) para que sean procesados por la máquina de estados. 
/// </summary>

public sealed class EnvironmentObserver : MonoBehaviour 
{
    private KnockbackData? _pendingKnockback;
    public bool HasKnockback => _pendingKnockback.HasValue;

    private void OnTriggerEnter(Collider other) 
    {
        ProcessImpact(other);
    }

    private void OnCollisionEnter(Collision collision) 
    {
        ProcessImpact(collision.collider);
    }

    private void ProcessImpact(Collider other)
    {
        if (other.TryGetComponent<IImpactSource>(out var source)) 
        {
            _pendingKnockback = source.GetKnockbackData(transform.position);
        }
    }

    public KnockbackData PeekKnockback()
    {
        return _pendingKnockback.GetValueOrDefault();
    }

    public KnockbackData ConsumeKnockback() 
    {
        if (!_pendingKnockback.HasValue) return default;
        
        var data = _pendingKnockback.Value;
        _pendingKnockback = null;
        return data;
    }
}