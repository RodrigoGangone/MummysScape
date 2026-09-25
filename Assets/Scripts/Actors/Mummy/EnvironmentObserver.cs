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
        // 1. Si ya tenemos un knockback pendiente esperando ser procesado, ignoramos los nuevos choques
        if (HasKnockback) return; 

        if (other.TryGetComponent<IImpactSource>(out var source)) 
        {
            var data = source.GetKnockbackData(transform.position);
        
            // 2. Filtro de seguridad: Solo guardamos el impacto si realmente tiene duración
            if (data.Duration > 0f)
            {
                _pendingKnockback = data;
            }
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