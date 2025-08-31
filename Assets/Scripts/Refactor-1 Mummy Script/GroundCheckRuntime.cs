using UnityEngine;

/// <summary>
/// GroundCheckRuntime
/// Chequeo de suelo mediante un único SphereCast hacia abajo (estable en bordes/pendientes).
/// - Filtra por LayerMask (usar "Floor").
/// - NonAlloc para evitar GC.
/// - Devuelve bool via IsGrounded y, opcionalmente, info completa via TryGetGround.
/// - Incluye límite de pendiente y Gizmos para debug en la Scene.
/// </summary>
[DisallowMultipleComponent]
public sealed class GroundCheckRuntime : MonoBehaviour
{
    [Header("Layer")]
    [Tooltip("Asignar únicamente la capa 'Floor'.")]
    [SerializeField] private LayerMask _groundMask = 0; // Seteá 'Floor' en el Inspector

    [Header("Probe Geometry")]
    [Tooltip("Offset desde el pivot del player al centro de la sonda (ideal: un poco por encima del piso).")]
    [SerializeField] private Vector3 _originOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("Radio del pie para el SphereCast.")]
    [Min(0f)][SerializeField] private float _footRadius = 0.2f;

    [Tooltip("Cuánto 'busca' hacia abajo el SphereCast.")]
    [Min(0f)][SerializeField] private float _castDistance = 0.4f;

    [Header("Slope")]
    [Tooltip("Ángulo máximo (en grados) para considerar 'suelo'. 0 = horizontal, 90 = todo.")]
    [Range(0f, 90f)][SerializeField] private float _maxGroundAngle = 90f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _hitColor = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color _missColor = new(1f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private Color _normalColor = new(0.2f, 0.6f, 1f, 0.9f);

    // Cache para NonAlloc (evita alocaciones por frame)
    private readonly RaycastHit[] _hits = new RaycastHit[4];

    // Para gizmos
    private bool _lastHit;
    private Vector3 _lastOrigin, _lastEnd, _lastPoint, _lastNormal;

    public struct GroundInfo
    {
        public bool hit;
        public Vector3 point;
        public Vector3 normal;
        public float distance;
        public Collider collider;
    }

    /// <summary>Conveniencia: solo bool grounded (usa TryGetGround interno).</summary>
    public bool IsGrounded(Transform tf) => TryGetGround(tf, out _);

    /// <summary>
    /// SphereCast hacia abajo. Si hay varios hits, devuelve el más cercano que cumpla con el límite de pendiente.
    /// </summary>
    private bool TryGetGround(Transform tf, out GroundInfo info)
    {
        var origin = tf.position + _originOffset;
        var dir = Vector3.down;

        _lastOrigin = origin;
        _lastEnd = origin + dir * _castDistance;

        var best = new RaycastHit { distance = float.MaxValue };
        int count = Physics.SphereCastNonAlloc(
            origin, _footRadius, dir, _hits, _castDistance, _groundMask, QueryTriggerInteraction.Ignore);

        bool anyValid = false;
        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (IsValidSlope(h.normal) && h.distance < best.distance)
            {
                best = h;
                anyValid = true;
            }
        }

        _lastHit = anyValid;
        _lastPoint = anyValid ? best.point : _lastEnd;
        _lastNormal = anyValid ? best.normal : Vector3.up;

        info = new GroundInfo
        {
            hit = anyValid,
            point = _lastPoint,
            normal = _lastNormal,
            distance = anyValid ? best.distance : _castDistance,
            collider = anyValid ? best.collider : null
        };
        return info.hit;
    }

    private bool IsValidSlope(in Vector3 normal)
    {
        if (_maxGroundAngle >= 89.99f) return true;
        float angle = Vector3.Angle(normal, Vector3.up);
        return angle <= _maxGroundAngle;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        // Dibujamos usando la última consulta si existe; si no, usamos el estado actual del transform.
        var tf = transform;
        var origin = (_lastOrigin == Vector3.zero) ? tf.position + _originOffset : _lastOrigin;
        var end = (_lastEnd == Vector3.zero) ? origin + Vector3.down * _castDistance : _lastEnd;

        // Línea del cast
        Gizmos.color = _lastHit ? _hitColor : _missColor;
        Gizmos.DrawLine(origin, end);

        // Esferas de inicio/fin
        Gizmos.DrawWireSphere(origin, _footRadius);
        Gizmos.DrawWireSphere(end, _footRadius);

        // Punto/normal si hay hit
        if (_lastHit)
        {
            Gizmos.DrawSphere(_lastPoint, 0.035f);
            Gizmos.color = _normalColor;
            Gizmos.DrawLine(_lastPoint, _lastPoint + _lastNormal * 0.35f);
        }
    }
}