using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Resuelve la cara activa de una caja empujable (colliders hijo con trigger)
/// y entrega PushInfo con la normal proyectada al plano XZ y el eje de
/// desplazamiento (±X o ±Z). También aplica el movimiento usando Rigidbody.
/// </summary>
public sealed class BoxPushAttract : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private BoxCollider _mainCollider;

    [Header("Debug")]
    [SerializeField] private bool _drawFaceGizmos;
    [SerializeField] private Color _faceGizmoColor = new(0.2f, 0.6f, 1f, 0.65f);

    private readonly Dictionary<Collider, FaceData> _faces = new();

    private void Reset()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _mainCollider = GetComponent<BoxCollider>();
    }

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_mainCollider == null) _mainCollider = GetComponent<BoxCollider>();
        CacheFaces();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_mainCollider == null) _mainCollider = GetComponent<BoxCollider>();
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

        Vector3 axisWorld = normalWorld; // Se mueve en la misma dirección que la normal (±X o ±Z).
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
            info = new PushInfo(this, faceCollider, face.Normal, face.Axis, face.FaceCenter, contactPoint, Mathf.Max(0f, playerDistance));
            return true;
        }

        info = default;
        return false;
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

    public void Move(Vector3 displacement)
    {
        if (displacement.sqrMagnitude <= 0f) return;

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
