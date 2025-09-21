using UnityEngine;

/// <summary>
/// InteractionRuntime
/// Rehace el chequeo de PUSH con doble raycast ("brazos").
/// - Lanza 2 rayos frontales a mitad de altura del player, separados 0.5f.
/// - Ambos deben tocar la misma cara (collider trigger) de un BoxPushAttract en layer Interactable.
/// - Valida que haya input hacia adelante para cachear un PushInfo.
/// - Dibuja gizmos en Scene para depurar (verde si ambos rayos son válidos, rojo en caso contrario).
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionRuntime : MonoBehaviour
{
    [Header("Push Probe")]
    [SerializeField] private CapsuleCollider _bodyCollider;
    [SerializeField] private LayerMask _pushLayer;
    [SerializeField, Min(0f)] private float _rayLength = 1f;
    [SerializeField, Min(0f)] private float _raySeparation = 0.5f;
    [Tooltip("Dot mínimo (0..1) entre input proyectado y forward para permitir Push.")]
    [Range(0f, 1f)][SerializeField] private float _minForwardDot = 0.4f;
    [SerializeField, Min(0f)] private float _playerFacePadding = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _validColor = new(0.2f, 1f, 0.3f, 0.9f);
    [SerializeField] private Color _invalidColor = new(1f, 0.25f, 0.25f, 0.7f);

    private static readonly RaycastHit[] s_RaycastHits = new RaycastHit[8];

    private PushInfo? _cachedPush;
    private GizmoData _gizmo;
    private float _playerDistance;

    private void Reset()
    {
        _bodyCollider = GetComponent<CapsuleCollider>();
        if (_pushLayer == 0) _pushLayer = LayerMask.GetMask("Interactable");
    }

    private void Awake()
    {
        if (_bodyCollider == null) _bodyCollider = GetComponent<CapsuleCollider>();
        if (_pushLayer == 0) _pushLayer = LayerMask.GetMask("Interactable");
        RecomputePlayerDistance();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_bodyCollider == null) _bodyCollider = GetComponent<CapsuleCollider>();
        if (_pushLayer == 0) _pushLayer = LayerMask.GetMask("Interactable");
        RecomputePlayerDistance();
    }
#endif

    private void RecomputePlayerDistance()
    {
        float radius = _bodyCollider ? _bodyCollider.radius : 0.4f;
        _playerDistance = Mathf.Max(0.1f, radius + _playerFacePadding);
    }

    public bool TryGetPushInfo(Transform tf, Vector2 rawMove, Vector3 worldMoveDir, out PushInfo info)
    {
        info = default;
        Vector3 forward = Vector3.ProjectOnPlane(tf.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.ProjectOnPlane(tf.right, Vector3.up);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        right.Normalize();

        Vector3 originCenter = GetRayOrigin(tf);
        UpdateGizmo(originCenter, forward, right, default, false, default, false);

        Vector3 horizontalMove = Vector3.ProjectOnPlane(worldMoveDir, Vector3.up);
        if (horizontalMove.sqrMagnitude < 0.0001f)
        {
            _cachedPush = null;
            return false;
        }

        horizontalMove.Normalize();
        if (Vector3.Dot(horizontalMove, forward) < _minForwardDot)
        {
            _cachedPush = null;
            return false;
        }

        Vector3 halfOffset = right * (_raySeparation * 0.5f);
        Vector3 originRight = originCenter + halfOffset;
        Vector3 originLeft = originCenter - halfOffset;

        bool hitRight = TryFindFaceHit(originRight, forward, out var rayRight, out var rightBox);
        bool hitLeft = TryFindFaceHit(originLeft, forward, out var rayLeft, out var leftBox);

        UpdateGizmo(originCenter, forward, right, rayLeft, hitLeft, rayRight, hitRight);

        if (!hitRight || !hitLeft)
        {
            _cachedPush = null;
            return false;
        }

        if (rayRight.collider == null || rayLeft.collider == null)
        {
            _cachedPush = null;
            return false;
        }

        if (rayRight.collider != rayLeft.collider)
        {
            _cachedPush = null;
            return false;
        }

        if (rightBox != leftBox)
        {
            _cachedPush = null;
            return false;
        }

        var target = rightBox;
        Vector3 contact = (rayLeft.point + rayRight.point) * 0.5f;
        if (!target.TryBuildInfo(rayRight.collider, _playerDistance, contact, out var built))
        {
            _cachedPush = null;
            return false;
        }

        _cachedPush = built;
        info = built;
        return true;
    }

    public bool TryGetCachedPush(out PushInfo info)
    {
        if (_cachedPush.HasValue)
        {
            info = _cachedPush.Value;
            return true;
        }

        info = default;
        return false;
    }

    public void ClearPushCache() => _cachedPush = null;

    private Vector3 GetRayOrigin(Transform tf)
    {
        if (_bodyCollider)
        {
            var bounds = _bodyCollider.bounds;
            return new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        }

        return tf.position + Vector3.up * 0.5f;
    }

    private void UpdateGizmo(Vector3 center, Vector3 forward, Vector3 right, in RaycastHit left, bool hitLeft, in RaycastHit rightHit, bool hitRight)
    {
        Vector3 halfOffset = right * (_raySeparation * 0.5f);
        _gizmo.originLeft = center - halfOffset;
        _gizmo.originRight = center + halfOffset;
        _gizmo.direction = forward;
        _gizmo.leftHit = hitLeft;
        _gizmo.rightHit = hitRight;
        _gizmo.leftPoint = hitLeft ? left.point : _gizmo.originLeft + forward * _rayLength;
        _gizmo.rightPoint = hitRight ? rightHit.point : _gizmo.originRight + forward * _rayLength;
    }

    /// <summary>
    /// Busca el primer trigger válido de BoxPushAttract golpeado por el rayo, descartando colliders físicos.
    /// </summary>
    private bool TryFindFaceHit(Vector3 origin, Vector3 direction, out RaycastHit hit, out BoxPushAttract box)
    {
        hit = default;
        box = null;

        int hitCount = Physics.RaycastNonAlloc(origin, direction, s_RaycastHits, _rayLength, _pushLayer, QueryTriggerInteraction.Collide);
        if (hitCount <= 0)
            return false;

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            var candidate = s_RaycastHits[i];
            var collider = candidate.collider;
            if (collider == null || !collider.isTrigger)
                continue;

            var attract = collider.GetComponentInParent<BoxPushAttract>();
            if (attract == null || !attract.IsFaceCollider(collider))
                continue;

            if (candidate.distance >= bestDistance)
                continue;

            bestDistance = candidate.distance;
            hit = candidate;
            box = attract;
        }

        return box != null;
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        Color leftColor = _gizmo.leftHit ? _validColor : _invalidColor;
        Color rightColor = _gizmo.rightHit ? _validColor : _invalidColor;

        Gizmos.color = leftColor;
        Gizmos.DrawLine(_gizmo.originLeft, _gizmo.leftPoint);
        Gizmos.DrawSphere(_gizmo.leftPoint, 0.03f);

        Gizmos.color = rightColor;
        Gizmos.DrawLine(_gizmo.originRight, _gizmo.rightPoint);
        Gizmos.DrawSphere(_gizmo.rightPoint, 0.03f);
    }

    private struct GizmoData
    {
        public Vector3 originLeft;
        public Vector3 originRight;
        public Vector3 direction;
        public Vector3 leftPoint;
        public Vector3 rightPoint;
        public bool leftHit;
        public bool rightHit;
    }
}
