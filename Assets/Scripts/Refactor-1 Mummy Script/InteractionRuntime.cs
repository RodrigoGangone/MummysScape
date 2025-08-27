using UnityEngine;

/// <summary>
/// InteractionRuntime
/// Provee utilidades de interacción del jugador: búsqueda de target para atraer, parámetros y LayerMasks.
/// </summary>
public sealed class InteractionRuntime : MonoBehaviour
{
    [Header("Attract")]
    [SerializeField] private LayerMask _attractMask;
    [SerializeField] private float _attractDistance = 4f;
    [SerializeField] private float _attractRadius = 0.5f;
    [SerializeField] private float _pullStrength = 25f;
    [SerializeField] private float _pullMaxSpeed = 6f;
    [SerializeField] private float _stopDistance = 1.25f;

    public float PullStrength => _pullStrength;
    public float PullMaxSpeed => _pullMaxSpeed;
    public float StopDistance => _stopDistance;

    /// <summary>
    /// Busca un IAttractable al frente del origen. Usa SphereCast para tolerancia.
    /// </summary>
    public bool TryFindAttractable(Transform origin, out IAttractable target, out RaycastHit hit)
    {
        Vector3 start = origin.position + Vector3.up * 0.6f;
        Vector3 dir   = origin.forward;
        bool ok = Physics.SphereCast(start, _attractRadius, dir, out hit, _attractDistance, _attractMask, QueryTriggerInteraction.Ignore);
        target = ok ? hit.collider.GetComponentInParent<IAttractable>() : null;
        return ok && target != null;
    }
}