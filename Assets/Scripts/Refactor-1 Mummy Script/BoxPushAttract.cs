using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Resuelve la cara activa de una caja empujable (colliders hijo con trigger)
/// y entrega PushInfo con la normal proyectada al plano XZ y el eje de
/// desplazamiento (±X o ±Z). Expone TryMoveAlongAxis para trasladar la caja
/// bloqueando paredes mediante BoxCast y aplica el movimiento con Rigidbody
/// o transform.
/// </summary>
public sealed class BoxPushAttract : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private BoxCollider _mainCollider;

    [Header("Movement Constraints")]
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField, Range(0f, 0.2f)] private float _castPadding = 0.02f;
    [SerializeField, Range(0f, 1f)] private float _minBlockNormalDot = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool _drawFaceGizmos;
    [SerializeField] private Color _faceGizmoColor = new(0.2f, 0.6f, 1f, 0.65f);

    private readonly Dictionary<Collider, FaceData> _faces = new();
    private static readonly RaycastHit[] s_BoxCastHits = new RaycastHit[8];

    private void Reset()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _mainCollider = GetComponent<BoxCollider>();
        if (_obstacleMask == 0) _obstacleMask = DefaultObstacleMask();
    }

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_mainCollider == null) _mainCollider = GetComponent<BoxCollider>();
        if (_obstacleMask == 0) _obstacleMask = DefaultObstacleMask();
        CacheFaces();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_mainCollider == null) _mainCollider = GetComponent<BoxCollider>();
        if (_obstacleMask == 0) _obstacleMask = DefaultObstacleMask();
        CacheFaces();
    }
#endif

    private void CacheFaces()
    {
        _faces.Clear();
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col == null || col == _mainCollider || !col.enabled || !col.isTrigger) continue;
            _faces[col] = BuildFaceData(col);
        }
    }

    private FaceData BuildFaceData(Collider trigger)
    {
        // Usamos forward local del trigger para la normal; fallback = vector hacia su centro.
        Vector3 localForward = transform.InverseTransformDirection(trigger.transform.forward);
        if (localForward.sqrMagnitude < 0.0001f)
        {
            Vector3 localCenter = transform.InverseTransformPoint(trigger.bounds.center);
            Vector3 mainCenter = _mainCollider ? transform.InverseTransformPoint(_mainCollider.bounds.center) : Vector3.zero;
            localForward = (localCenter - mainCenter);
        }

        Vector3 snappedLocal = SnapToAxis(localForward);
        Vector3 normalWorld = transform.TransformDirection(snappedLocal).normalized;
        normalWorld = Vector3.ProjectOnPlane(normalWorld, Vector3.up).normalized;
        if (normalWorld.sqrMagnitude < 0.0001f) normalWorld = Vector3.forward;

        Vector3 axisWorld = -normalWorld; // La caja avanza alejándose del jugador (±X o ±Z).
        Vector3 faceCenter = trigger.bounds.center;

        return new FaceData(trigger, normalWorld, axisWorld, faceCenter);
    }

    private static Vector3 SnapToAxis(Vector3 v)
    {
        v = Vector3.ProjectOnPlane(v, Vector3.up);
        if (v.sqrMagnitude < 0.0001f) return Vector3.forward;
        float absX = Mathf.Abs(v.x);
        float absZ = Mathf.Abs(v.z);
        if (absX >= absZ)
            return new Vector3(Mathf.Sign(v.x), 0f, 0f);
        return new Vector3(0f, 0f, Mathf.Sign(v.z));
    }

    public bool TryBuildInfo(Collider faceCollider, float playerDistance, Vector3 contactPoint, out PushInfo info)
    {
        if (_faces.TryGetValue(faceCollider, out var face))
        {
            info = new PushInfo(
                this,
                faceCollider,
                face.Normal,
                face.Axis,
                face.FaceCenter,
                contactPoint,
                Mathf.Max(0f, playerDistance),
                GetHorizontalCenter());
            return true;
        }

        info = default;
        return false;
    }

    /// <summary>
    /// Centro del volumen principal de la caja (los consumidores pueden proyectarlo a XZ).
    /// </summary>
    public Vector3 GetHorizontalCenter()
    {
        return _mainCollider != null ? _mainCollider.bounds.center : transform.position;
    }

    /// <summary>
    /// Determina si el collider recibido pertenece a una cara empujable registrada.
    /// </summary>
    public bool IsFaceCollider(Collider faceCollider)
    {
        if (faceCollider == null)
            return false;

        if (_faces.Count == 0)
            CacheFaces();

        return _faces.ContainsKey(faceCollider);
    }

    /// <summary>
    /// Intenta desplazar la caja siguiendo un eje horizontal (±X o ±Z).
    /// Respeta colisiones frontales usando BoxCast; retorna false si no se movió.
    /// </summary>
    public bool TryMoveAlongAxis(Vector3 axis, float distance, out Vector3 displacement)
    {
        displacement = Vector3.zero;

        if (distance <= 0f)
            return false;

        Vector3 snappedAxis = SnapToAxis(axis);
        if (snappedAxis.sqrMagnitude < 0.5f)
            return false;

        Vector3 direction = snappedAxis.normalized;
        float allowedDistance = ComputeAllowedDistance(direction, distance);
        if (allowedDistance <= 0f)
            return false;

        displacement = direction * allowedDistance;
        ApplyDisplacement(displacement);
        return true;
    }

    /// <summary>
    /// Mantiene compatibilidad moviendo sin validaciones externas.
    /// </summary>
    public void Move(Vector3 displacement)
    {
        if (displacement.sqrMagnitude <= 0f)
            return;

        ApplyDisplacement(displacement);
    }

    private void ApplyDisplacement(Vector3 displacement)
    {
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.MovePosition(_rigidbody.position + displacement);
        }
        else
        {
            transform.position += displacement;
        }
    }

    private float ComputeAllowedDistance(Vector3 direction, float requestedDistance)
    {
        if (_mainCollider == null)
            return requestedDistance;

        if (requestedDistance <= 0f)
            return 0f;

        Transform colTransform = _mainCollider.transform;
        Quaternion orientation = colTransform.rotation;
        Vector3 halfExtents = Vector3.Scale(_mainCollider.size * 0.5f, AbsVector(colTransform.lossyScale));
        halfExtents = Vector3.Max(halfExtents - Vector3.one * _castPadding, Vector3.one * 0.001f);
        Vector3 origin = colTransform.TransformPoint(_mainCollider.center);

        float castDistance = requestedDistance + _castPadding;
        float allowedDistance = requestedDistance;
        int hitCount = Physics.BoxCastNonAlloc(
            origin,
            halfExtents,
            direction,
            s_BoxCastHits,
            orientation,
            castDistance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            var hit = s_BoxCastHits[i];
            var collider = hit.collider;
            if (collider == null)
                continue;

            if (collider.transform == colTransform)
                continue;

            if (collider.transform.IsChildOf(transform))
                continue;

            if (collider.attachedRigidbody != null && collider.attachedRigidbody == _rigidbody)
                continue;

            if (collider.isTrigger)
                continue;

            float facing = Vector3.Dot(hit.normal, -direction);
            if (facing < _minBlockNormalDot)
                continue;

            float candidate = Mathf.Max(0f, hit.distance - _castPadding);
            if (candidate < allowedDistance)
                allowedDistance = candidate;
        }

        return Mathf.Clamp(allowedDistance, 0f, requestedDistance);
    }

    private static Vector3 AbsVector(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private static int DefaultObstacleMask() => Physics.DefaultRaycastLayers;

    private void OnDrawGizmosSelected()
    {
        if (!_drawFaceGizmos) return;
        if (_faces.Count == 0) CacheFaces();

        Gizmos.color = _faceGizmoColor;
        foreach (var face in _faces.Values)
        {
            Gizmos.DrawWireSphere(face.FaceCenter, 0.05f);
            Gizmos.DrawLine(face.FaceCenter, face.FaceCenter + face.Normal * 0.4f);
        }
    }

    private readonly struct FaceData
    {
        public readonly Collider Collider;
        public readonly Vector3 Normal;
        public readonly Vector3 Axis;
        public readonly Vector3 FaceCenter;

        public FaceData(Collider collider, Vector3 normal, Vector3 axis, Vector3 faceCenter)
        {
            Collider = collider;
            Normal = normal;
            Axis = axis;
            FaceCenter = faceCenter;
        }
    }
}
