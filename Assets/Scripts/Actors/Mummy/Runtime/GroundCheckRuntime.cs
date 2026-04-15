using UnityEngine;
using System;

[DisallowMultipleComponent]
public sealed class GroundCheckRuntime : MonoBehaviour
{
    public enum TerrainType { None, Default, Sand }

    [Serializable]
    public struct TerrainConfig
    {
        public TerrainType Type;
        public LayerMask Mask;
        public Color DebugColor;
    }

    [Header("Detection Layers")]
    [Tooltip("Capa global que bloquea el movimiento (debe incluir todas las capas de abajo).")]
    [SerializeField] private LayerMask _groundMask = 0;

    [Tooltip("Configura aquí qué capas corresponden a qué terreno.")]
    [SerializeField] private TerrainConfig[] _terrains;

    [Header("Probe Geometry")]
    [SerializeField] private Vector3 _originOffset = new(0f, 0.5f, 0f);
    [SerializeField] private float _footRadius = 0.52f;
    [SerializeField] private float _castDistance = 0.1f;

    [Header("Slope")]
    [Range(0f, 90f)] [SerializeField] private float _maxGroundAngle = 75f;

    // Resultados públicos
    public bool IsGrounded { get; private set; }
    public TerrainType CurrentTerrain { get; private set; }

    private readonly RaycastHit[] _hits = new RaycastHit[4];
    private Vector3 _lastOrigin, _lastEnd, _lastPoint, _lastNormal;
    private Color _currentDebugColor = Color.red;

    private struct GroundResult
    {
        public bool hit;
        public TerrainType terrain;
        public Vector3 point;
        public Vector3 normal;
        public Color color;
    }

    /// <summary>
    /// Actualiza el estado del suelo. Llamar desde el PlayerContext o un Update centralizado.
    /// </summary>
    public bool CheckGround(Transform tf)
    {
        var result = EvaluateGround(tf);
        
        IsGrounded = result.hit;
        CurrentTerrain = result.terrain;
        _currentDebugColor = result.color;

        return IsGrounded;
    }

    private GroundResult EvaluateGround(Transform tf)
    {
        Vector3 origin = tf.position + _originOffset;
        Vector3 dir = Vector3.down;

        _lastOrigin = origin;
        _lastEnd = origin + dir * _castDistance;

        int count = Physics.SphereCastNonAlloc(
            origin, _footRadius, dir, _hits, _castDistance, _groundMask, QueryTriggerInteraction.Ignore
        );

        RaycastHit bestHit = default;
        bool foundValid = false;
        float minDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (!IsSlopeValid(h.normal)) continue;

            if (h.distance < minDistance)
            {
                minDistance = h.distance;
                bestHit = h;
                foundValid = true;
            }
        }

        if (foundValid)
        {
            _lastPoint = bestHit.point;
            _lastNormal = bestHit.normal;

            // Identificar el terreno por Layer
            var terrainInfo = GetTerrainFromLayer(bestHit.collider.gameObject.layer);

            return new GroundResult
            {
                hit = true,
                terrain = terrainInfo.Type,
                point = bestHit.point,
                normal = bestHit.normal,
                color = terrainInfo.DebugColor
            };
        }

        // Si no hay hit
        _lastPoint = _lastEnd;
        _lastNormal = Vector3.up;
        return new GroundResult { hit = false, terrain = TerrainType.None, color = Color.red };
    }

    private TerrainConfig GetTerrainFromLayer(int layer)
    {
        // Buscamos en nuestro array de configuraciones
        for (int i = 0; i < _terrains.Length; i++)
        {
            if (((1 << layer) & _terrains[i].Mask) != 0)
            {
                return _terrains[i];
            }
        }
        // Terreno por defecto si no está en la lista pero el GroundMask lo detectó
        return new TerrainConfig { Type = TerrainType.Default, DebugColor = Color.green };
    }

    private bool IsSlopeValid(Vector3 normal) => Vector3.Angle(normal, Vector3.up) <= _maxGroundAngle;

    #region Gizmos
    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        Vector3 origin = (_lastOrigin == Vector3.zero) ? transform.position + _originOffset : _lastOrigin;
        Vector3 end = (_lastEnd == Vector3.zero) ? origin + Vector3.down * _castDistance : _lastEnd;

        Gizmos.color = _currentDebugColor;
        Gizmos.DrawWireSphere(origin, _footRadius);
        Gizmos.DrawLine(origin, end);

        if (IsGrounded)
        {
            Gizmos.DrawSphere(_lastPoint, 0.1f);
            Gizmos.DrawLine(_lastPoint, _lastPoint + _lastNormal * 0.5f);
        }
    }
    #endregion
}