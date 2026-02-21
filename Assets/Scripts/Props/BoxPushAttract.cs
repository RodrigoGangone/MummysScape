using Cinemachine;
using UnityEngine;

/// <summary> 
/// Lógica de Caja Dinámica: Controla el comportamiento físico de objetos empujables y atraíbles, 
/// gestionando restricciones de movimiento XZ y validación de soporte de suelo multirrayo. 
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public sealed class BoxPushAttract : MonoBehaviour
{
    [Header("Fricción / Física")] [SerializeField]
    private PhysicMaterial _materialBajaFriccion;

    [Header("Ground Check")] [SerializeField]
    private LayerMask _groundMask;

    [SerializeField] private LayerMask _sandMask;

    [SerializeField] private float _groundDistance = 0.25f;
    [SerializeField] private float _cornerInset = 0.01f;
    [SerializeField] private float _cornerOutset = 0.025f;
    [Range(0, 4)] [SerializeField] private int _minSupportedCorners = 1;
    [SerializeField] private bool _useBoxCast = false;
    [SerializeField] private float _boxCastInset = 0.01f;
    
    [Header("Fx")] [SerializeField] public FxBank bank;

    private CinemachineImpulseSource _impulseSource;
    [SerializeField] private float _shakeForce;

    private ParticleSystem _fxImpact;

    private WrapHandler _wrapHandler;
    private Rigidbody _rb;
    private BoxCollider _col;

    private static readonly RigidbodyConstraints IdleConstraints =
        RigidbodyConstraints.FreezeRotation |
        RigidbodyConstraints.FreezePositionX |
        RigidbodyConstraints.FreezePositionZ;

    private static readonly RigidbodyConstraints FreeXZConstraints =
        RigidbodyConstraints.FreezeRotation;

    private readonly Vector3[] _gcOrigins = new Vector3[4];
    private readonly bool[] _gcHits = new bool[4];
    private readonly Vector3[] _gcHitPoints = new Vector3[4];
    private bool _gcSupported;
    private int _gcSupportedCount;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<BoxCollider>();
        _fxImpact = GetComponentInChildren<ParticleSystem>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        _wrapHandler = GetComponent<WrapHandler>();

        if (_materialBajaFriccion && _col) _col.sharedMaterial = _materialBajaFriccion;

        _rb.useGravity = true;
        _rb.isKinematic = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _rb.constraints = IdleConstraints;
    }

    public float GetAttachDuration() => _wrapHandler != null ? _wrapHandler.WrapDuration : 0f;

    public void SetPushAttractMode(bool enabled, bool useBandages = true)
    {
        if (enabled && useBandages)
        {
            _wrapHandler.Wrap();
        }

        _rb.constraints = enabled ? FreeXZConstraints : IdleConstraints;

        if (!enabled)
        {
            var v = _rb.velocity;
            v.x = 0f;
            v.z = 0f;
            _rb.velocity = v;
        }
    }

    public bool IsGroundedForPushAttract()
    {
        if (_useBoxCast)
            return GroundCheckBoxCast();
        return GroundCheckCorners(out _);
    }

    public void MoveBy(in Vector3 deltaWorld)
    {
        if ((_rb.constraints & (RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ)) != 0)
            return;
        _rb.MovePosition(_rb.position + new Vector3(deltaWorld.x, 0f, deltaWorld.z));
    }

    public void StopImmediate()
    {
        _rb.velocity = Vector3.zero;
    }

    private bool GroundCheckCorners(out int supported)
    {
        supported = 0;
        if (!_col) return false;

        Vector3 lossy = transform.lossyScale;
        Vector3 ext = Vector3.Scale(_col.size * 0.5f, lossy);
        Vector3 center = transform.TransformPoint(_col.center);

        float yBase = center.y - ext.y + 0.01f;
        Vector3 right = transform.right;
        Vector3 fwd = transform.forward;

        float ex = Mathf.Max(0f, ext.x + _cornerOutset - _cornerInset);
        float ez = Mathf.Max(0f, ext.z + _cornerOutset - _cornerInset);

        Vector3 rx = right * ex;
        Vector3 fz = fwd * ez;

        Vector3[] localOffsets =
        {
            -rx - fz, // back-left
            -rx + fz, // front-left
            rx - fz, // back-right
            rx + fz // front-right
        };

        _gcSupportedCount = 0;
        _gcSupported = false;

        for (int i = 0; i < 4; i++)
        {
            Vector3 origin = new(center.x + localOffsets[i].x, yBase, center.z + localOffsets[i].z);
            _gcOrigins[i] = origin;
            _gcHits[i] = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _groundDistance, _groundMask,
                QueryTriggerInteraction.Ignore);
            _gcHitPoints[i] = _gcHits[i] ? hit.point : origin + Vector3.down * _groundDistance;
            if (_gcHits[i]) _gcSupportedCount++;
        }

        _gcSupported = _gcSupportedCount >= _minSupportedCorners;
        return _gcSupported;
    }

    public float HorizontalDistanceTo(Vector3 worldPoint)
    {
        if (_col == null) _col = GetComponent<BoxCollider>();
        Vector3 query = new(worldPoint.x, _col.bounds.center.y, worldPoint.z);
        Vector3 closest = _col.ClosestPoint(query);
        Vector2 v = new(closest.x - query.x, closest.z - query.z);
        return v.magnitude;
    }

    private bool GroundCheckBoxCast()
    {
        if (!_col) return false;

        Bounds b = _col.bounds;
        Vector3 half = new(Mathf.Max(0.001f, b.extents.x - _boxCastInset), 0.01f,
            Mathf.Max(0.001f, b.extents.z - _boxCastInset));
        Vector3 origin = new(b.center.x, b.min.y + 0.05f, b.center.z);

        bool hit = Physics.BoxCast(origin, half, Vector3.down, out RaycastHit h, Quaternion.identity, _groundDistance,
            _groundMask, QueryTriggerInteraction.Ignore);
        _gcOrigins[0] = origin;
        _gcHits[0] = hit;
        _gcHitPoints[0] = hit ? h.point : origin + Vector3.down * _groundDistance;
        _gcSupported = hit;
        _gcSupportedCount = hit ? 4 : 0;
        return hit;
    }

    private void OnCollisionEnter(Collision other)
    {
        if ((_sandMask.value & (1 << other.gameObject.layer)) != 0)
        {
            bank.Play3D("HitBox", transform.position);
            _fxImpact.Play();
            _impulseSource.GenerateImpulse(_shakeForce);
        }
    }

    #region Gizmos

    [Header("Debug")] [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _okColor = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color _badColor = new(1f, 0.2f, 0.2f, 0.9f);
    
    private void OnDrawGizmosSelected() => OnDrawGizmos();

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;
        if (Application.isPlaying == false && TryGetComponent(out _col) == false) return;

        if (_useBoxCast)
        {
            Bounds b = _col.bounds;
            Vector3 size = new(Mathf.Max(0.001f, b.size.x - 2f * _boxCastInset), 0.02f,
                Mathf.Max(0.001f, b.size.z - 2f * _boxCastInset));
            Vector3 origin = new(b.center.x, b.min.y + 0.05f, b.center.z);

            GroundCheckBoxCast();

            Gizmos.color = _gcSupported ? _okColor : _badColor;
            Gizmos.DrawWireCube(new(origin.x, b.min.y + 0.01f, origin.z), size);
            Gizmos.DrawLine(_gcOrigins[0], _gcOrigins[0] + Vector3.down * _groundDistance);
            Gizmos.DrawSphere(_gcHitPoints[0], 0.03f);
            return;
        }

        GroundCheckCorners(out _);
        for (int i = 0; i < 4; i++)
        {
            Gizmos.color = _gcHits[i] ? _okColor : _badColor;
            Gizmos.DrawLine(_gcOrigins[i], _gcOrigins[i] + Vector3.down * _groundDistance);
            Gizmos.DrawSphere(_gcHitPoints[i], 0.03f);
        }
    }

    #endregion
}