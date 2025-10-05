using UnityEngine;

//// <summary>
//// BoxPushAttract
//// - Caja dinámica: fuera de Push congela XZ; en Push libera XZ.
//// - GroundCheck: por defecto 4 raycasts en las esquinas (robusto en suelos 1x1).
////   Opcional: BoxCast cuadrado que replica la base.
//// - IsGroundedForPush() true si el soporte bajo la caja pertenece a _groundMask
////   (ej.: layers "Box", "Floor", "Interactable") y supera el umbral de esquinas soportadas.
//// - OnDrawGizmos: dibuja orígenes, rayos y color según estado (verde = soportado).
//// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public sealed class BoxPushAttract : MonoBehaviour
{
    [Header("Fricción / Física")]
    [SerializeField] private PhysicMaterial _materialBajaFriccion;

    [Header("Ground Check")]
    [SerializeField, Tooltip("Layers que cuentan como 'suelo' (Box/Floor/Interactable).")]
    private LayerMask _groundMask;
    [SerializeField, Tooltip("Altura del raycast hacia abajo desde la base.")]
    private float _groundDistance = 0.25f;
    [SerializeField, Tooltip("Inset lateral para evitar caer justo en la arista del collider.")]
    private float _cornerInset = 0.01f;
    [SerializeField, Tooltip("Extiende el rayo HACIA AFUERA de la base (para empujar hasta que salga y caiga).")]
    private float _cornerOutset = 0.025f;
    [SerializeField, Tooltip("Esquinas mínimas con soporte para dar OK (0-4).")]
    [Range(0,4)] private int _minSupportedCorners = 1;
    [SerializeField, Tooltip("Alternativa: usar BoxCast en lugar de 4 raycasts.")]
    private bool _useBoxCast = false;
    [SerializeField, Tooltip("Reducción del tamaño del BoxCast respecto a la base.")]
    private float _boxCastInset = 0.01f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _okColor = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color _badColor = new(1f, 0.2f, 0.2f, 0.9f);

    private Rigidbody _rb;
    private BoxCollider _col;

    // Congelada en XZ cuando no empujamos; libre en XZ durante push.
    private static readonly RigidbodyConstraints IdleConstraints =
        RigidbodyConstraints.FreezeRotation |
        RigidbodyConstraints.FreezePositionX |
        RigidbodyConstraints.FreezePositionZ;

    private static readonly RigidbodyConstraints PushConstraints =
        RigidbodyConstraints.FreezeRotation;

    // Cache gizmos
    private readonly Vector3[] _gcOrigins = new Vector3[4];
    private readonly bool[] _gcHits = new bool[4];
    private readonly Vector3[] _gcHitPoints = new Vector3[4];
    private bool _gcSupported;
    private int _gcSupportedCount;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody>();
        _col = GetComponent<BoxCollider>();

        if (_materialBajaFriccion && _col) _col.sharedMaterial = _materialBajaFriccion;

        _rb.useGravity = true;
        _rb.isKinematic = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Al iniciar NO estamos empujando, por eso congelamos XZ
        _rb.constraints = IdleConstraints;
    }

    /// <summary>Habilita/deshabilita el modo push (libera o congela XZ).</summary>
    public void SetPushMode(bool enabled)
    {
        _rb.constraints = enabled ? PushConstraints : IdleConstraints;

        if (!enabled)
        {
            // por si venía con inercia horizontal
            var v = _rb.velocity;
            v.x = 0f; v.z = 0f;
            _rb.velocity = v;
        }
    }

    /// <summary>
    /// Devuelve true si la base de la caja está soportada por _groundMask
    /// según el método elegido (4 esquinas o BoxCast) y el umbral configurado.
    /// </summary>
    public bool IsGroundedForPush()
    {
        if (_useBoxCast)
            return GroundCheckBoxCast();
        return GroundCheckCorners(out _);
    }

    /// <summary>Mueve la caja por delta en XZ (solo tiene efecto en modo push).</summary>
    public void MoveBy(in Vector3 deltaWorld)
    {
        if ((_rb.constraints & (RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ)) != 0)
            return; // fuera de Push, ignoramos
        _rb.MovePosition(_rb.position + new Vector3(deltaWorld.x, 0f, deltaWorld.z));
    }

    public void StopImmediate() => _rb.velocity = Vector3.zero;

    // ---------- Ground Check (4 esquinas) ----------
    private bool GroundCheckCorners(out int supported)
    {
        supported = 0;
        if (!_col) return false;

        // Extents en mundo respetando escala
        Vector3 lossy = transform.lossyScale;
        Vector3 ext = Vector3.Scale(_col.size * 0.5f, lossy);
        Vector3 center = transform.TransformPoint(_col.center);

        // Base (un poquito arriba para no empezar "dentro" del piso)
        float yBase = center.y - ext.y + 0.01f;
        Vector3 right = transform.right;
        Vector3 fwd   = transform.forward;
        
        // aplicamos 'outset' (hacia afuera) y 'inset' (hacia adentro)
        float ex = Mathf.Max(0f, ext.x + _cornerOutset - _cornerInset);
        float ez = Mathf.Max(0f, ext.z + _cornerOutset - _cornerInset);

        Vector3 rx = right * ex;
        Vector3 fz = fwd   * ez;

        Vector3[] localOffsets =
        {
            -rx - fz, // back-left
            -rx + fz, // front-left
             rx - fz, // back-right
             rx + fz  // front-right
        };
        
        _gcSupportedCount = 0;
        _gcSupported = false;

        for (int i = 0; i < 4; i++)
        {
            Vector3 origin = new(center.x + localOffsets[i].x, yBase, center.z + localOffsets[i].z);
            _gcOrigins[i] = origin;
            _gcHits[i] = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _groundDistance, _groundMask, QueryTriggerInteraction.Ignore);
            _gcHitPoints[i] = _gcHits[i] ? hit.point : origin + Vector3.down * _groundDistance;
            if (_gcHits[i]) _gcSupportedCount++;
        }

        _gcSupported = _gcSupportedCount >= _minSupportedCorners;
        return _gcSupported;
    }

    // ---------- Ground Check (BoxCast) ----------
    private bool GroundCheckBoxCast()
    {
        if (!_col) return false;

        Bounds b = _col.bounds;
        // achico un poco el footprint para evitar falsos negativos por juntas
        Vector3 half = new(Mathf.Max(0.001f, b.extents.x - _boxCastInset), 0.01f, Mathf.Max(0.001f, b.extents.z - _boxCastInset));
        Vector3 origin = new(b.center.x, b.min.y + 0.05f, b.center.z);

        bool hit = Physics.BoxCast(origin, half, Vector3.down, out RaycastHit h, Quaternion.identity, _groundDistance, _groundMask, QueryTriggerInteraction.Ignore);
        // para gizmos reutilizo el slot 0
        _gcOrigins[0] = origin;
        _gcHits[0] = hit;
        _gcHitPoints[0] = hit ? h.point : origin + Vector3.down * _groundDistance;
        _gcSupported = hit;
        _gcSupportedCount = hit ? 4 : 0;
        return hit;
    }

    private void OnDrawGizmosSelected() => OnDrawGizmos();
    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        // recomputo el estado actual para visualizar bien en editor
        if (Application.isPlaying == false && TryGetComponent(out _col) == false) return;

        if (_useBoxCast)
        {
            // Dibujo footprint del BoxCast
            Bounds b = _col.bounds;
            Vector3 size = new(Mathf.Max(0.001f, b.size.x - 2f * _boxCastInset), 0.02f, Mathf.Max(0.001f, b.size.z - 2f * _boxCastInset));
            Vector3 origin = new(b.center.x, b.min.y + 0.05f, b.center.z);

            // recomputo para colores
            GroundCheckBoxCast();

            Gizmos.color = _gcSupported ? _okColor : _badColor;
            Gizmos.DrawWireCube(new(origin.x, b.min.y + 0.01f, origin.z), size);
            // rayo
            Gizmos.DrawLine(_gcOrigins[0], _gcOrigins[0] + Vector3.down * _groundDistance);
            Gizmos.DrawSphere(_gcHitPoints[0], 0.03f);
            return;
        }

        // 4 esquinas
        GroundCheckCorners(out _);
        for (int i = 0; i < 4; i++)
        {
            Gizmos.color = _gcHits[i] ? _okColor : _badColor;
            Gizmos.DrawLine(_gcOrigins[i], _gcOrigins[i] + Vector3.down * _groundDistance);
            Gizmos.DrawSphere(_gcHitPoints[i], 0.03f);
        }
    }
}