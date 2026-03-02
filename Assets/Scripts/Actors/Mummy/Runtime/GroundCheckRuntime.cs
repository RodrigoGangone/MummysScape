using UnityEngine;

/// <summary> 
/// Sistema de Detección de Suelo: Realiza un único SphereCast descendente para determinar si el personaje 
/// está apoyado en una superficie válida. Incluye un filtro de pendiente máxima (Slope) 
/// y utiliza optimización NonAlloc para garantizar un rendimiento sin recolección de basura por frame.
/// </summary>

[DisallowMultipleComponent]
public sealed class GroundCheckRuntime : MonoBehaviour
{
    [Header("Layer")] [Tooltip("Capas consideradas 'suelo' (ej: Floor, Box, Interactable).")]
    [SerializeField] private LayerMask _groundMask = 0;

    [Header("Probe Geometry")] [Tooltip("Offset desde el pivot del player al centro de la sonda.")] 
    [SerializeField] private Vector3 _originOffset = new(0f, 0.5f, 0f);

    [Tooltip("Radio de la esfera del pie para el SphereCast.")] [Min(0f)] 
    [SerializeField] private float _footRadius = 0.52f;

    [Tooltip("Alcance del SphereCast hacia abajo.")] [Min(0f)] 
    [SerializeField] private float _castDistance = 0f;

    [Header("Slope")]
    [Tooltip("Ángulo máximo (grados) para considerar 'suelo'. 90 = cualquier pendiente.")]
    [Range(0f, 90f)] [SerializeField] private float _maxGroundAngle = 75f;

    private readonly RaycastHit[] _hits = new RaycastHit[4];

    private bool _lastHit;
    private Vector3 _lastOrigin, _lastEnd, _lastPoint, _lastNormal;
    private float _lastRadius;

    private struct GroundInfo
    {
        public bool hit;
        public Vector3 point;
        public Vector3 normal;
        public float distance;
        public Collider collider;
    }

    public bool IsGrounded(Transform tf) => TryGetGround(tf, out _);

    private bool TryGetGround(Transform tf, out GroundInfo info)
    {
        Vector3 origin = tf.position + _originOffset;
        Vector3 dir = Vector3.down;

        _lastOrigin = origin;
        _lastEnd = origin + dir * _castDistance;
        _lastRadius = _footRadius;

        var best = new RaycastHit { distance = float.MaxValue };
        int count = Physics.SphereCastNonAlloc(
            origin,
            _footRadius,
            dir,
            _hits,
            _castDistance,
            _groundMask,
            QueryTriggerInteraction.Ignore
        );

        bool anyValid = false;
        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (!IsSlopeValid(h.normal)) continue;
            if (h.distance < best.distance)
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

    private bool IsSlopeValid(in Vector3 normal)
    {
        if (_maxGroundAngle >= 89.99f) return true;
        float angle = Vector3.Angle(normal, Vector3.up);
        return angle <= _maxGroundAngle;
    }

    #region Gizmos
    
    [Header("Debug")] 
    
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _hitColor = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color _missColor = new(1f, 0.2f, 0.2f, 0.7f);
    [SerializeField] private Color _normalColor = new(0.2f, 0.6f, 1f, 0.9f);

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        Vector3 origin = (_lastOrigin == Vector3.zero) ? transform.position + _originOffset : _lastOrigin;
        Vector3 end = (_lastEnd == Vector3.zero) ? origin + Vector3.down * _castDistance : _lastEnd;
        float radius = _lastRadius > 0f ? _lastRadius : _footRadius;

        Gizmos.color = _lastHit ? _hitColor : _missColor;
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.DrawLine(origin, end);

        if (_lastHit)
        {
            Gizmos.DrawSphere(_lastPoint, Mathf.Max(0.02f, radius * 0.15f));
            Gizmos.color = _normalColor;
            Gizmos.DrawLine(_lastPoint, _lastPoint + _lastNormal * (radius * 1.5f));
        }
    }

    #endregion
}