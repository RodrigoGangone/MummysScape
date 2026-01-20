using UnityEngine;

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

    // --- NUEVO: Permite leer los datos sin borrarlos ---
    public KnockbackData PeekKnockback()
    {
        return _pendingKnockback.GetValueOrDefault();
    }
    // --------------------------------------------------

    public KnockbackData ConsumeKnockback() 
    {
        if (!_pendingKnockback.HasValue) return default;
        
        var data = _pendingKnockback.Value;
        _pendingKnockback = null; // Aquí es donde HasKnockback se vuelve FALSE
        return data;
    }
}