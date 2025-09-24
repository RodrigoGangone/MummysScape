using UnityEngine;

/// <summary>
/// InteractionRuntime
/// Gestiona el chequeo de push con doble raycast frontal separados horizontalmente.
/// - Lanza dos raycast a mitad de altura, LayerMask "Interactable", para validar la cara tocada por ambos.
/// - Retorna el BoxPushAttract y la cara desde la que empuja la mummy.
/// - Dibuja Gizmos para depurar (rojo = sin hit, amarillo = hit inválido, verde = push válido).
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionRuntime : MonoBehaviour
{
    private const float GizmoSphereRadius = 0.05f;

    [Header("Push Raycasts")]
    [SerializeField] private float _rayLength = 1f;
    [SerializeField] private float _rayHeight = 0.75f;
    [SerializeField] private float _raySeparation = 0.5f;
    [SerializeField] private LayerMask _interactableMask = 0;

    private readonly RayDebugData[] _debugRays = new RayDebugData[2];
    private bool _debugHasValidHit;

    private void Awake()
    {
        EnsureInteractableMask();
    }

    private void OnValidate()
    {
        EnsureInteractableMask();
        _rayLength = Mathf.Max(0f, _rayLength);
        _raySeparation = Mathf.Max(0f, _raySeparation);
    }

    private void EnsureInteractableMask()
    {
        if (_interactableMask.value != 0)
        {
            return;
        }

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
        {
            _interactableMask = 1 << interactableLayer;
        }
    }

    /// <summary>
    /// Evalúa si los dos raycasts frontales golpean la misma cara de un BoxPushAttract.
    /// </summary>
    public bool TryGetPushTarget(out BoxPushAttract box, out BoxPushAttract.PushFace face)
    {
        return EvaluatePush(out box, out face, true);
    }

    private bool EvaluatePush(out BoxPushAttract box, out BoxPushAttract.PushFace face, bool recordDebug)
    {
        Vector3 forward = transform.forward.normalized;
        Vector3 right = transform.right;
        Vector3 originCenter = transform.position + transform.up * _rayHeight;
        float halfSeparation = Mathf.Max(0f, _raySeparation * 0.5f);

        Vector3 originLeft = originCenter - right * halfSeparation;
        Vector3 originRight = originCenter + right * halfSeparation;

        bool hasLeftHit = Physics.Raycast(originLeft, forward, out RaycastHit hitLeft, _rayLength, _interactableMask, QueryTriggerInteraction.Collide);
        bool hasRightHit = Physics.Raycast(originRight, forward, out RaycastHit hitRight, _rayLength, _interactableMask, QueryTriggerInteraction.Collide);

        if (recordDebug)
        {
            _debugRays[0] = RayDebugData.Create(originLeft, forward, _rayLength, hasLeftHit, hitLeft.point);
            _debugRays[1] = RayDebugData.Create(originRight, forward, _rayLength, hasRightHit, hitRight.point);
        }

        if (!hasLeftHit || !hasRightHit)
        {
            box = null;
            face = default;
            if (recordDebug)
            {
                _debugHasValidHit = false;
            }

            return false;
        }

        if (hitLeft.collider == null || hitRight.collider == null)
        {
            box = null;
            face = default;
            if (recordDebug)
            {
                _debugHasValidHit = false;
            }

            return false;
        }

        if (hitLeft.collider.transform != hitRight.collider.transform)
        {
            box = null;
            face = default;
            if (recordDebug)
            {
                _debugHasValidHit = false;
            }

            return false;
        }

        box = hitLeft.collider.GetComponentInParent<BoxPushAttract>();
        if (box == null)
        {
            face = default;
            if (recordDebug)
            {
                _debugHasValidHit = false;
            }

            return false;
        }

        if (!box.TryGetFace(hitLeft.collider, out face))
        {
            if (recordDebug)
            {
                _debugHasValidHit = false;
            }

            return false;
        }

        if (recordDebug)
        {
            _debugHasValidHit = true;
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        EvaluatePush(out _, out _, true);

        for (int i = 0; i < _debugRays.Length; i++)
        {
            RayDebugData ray = _debugRays[i];
            Gizmos.color = GetGizmoColor(ray, _debugHasValidHit);
            Vector3 endPoint = ray.HasHit ? ray.HitPoint : ray.Origin + ray.Direction * ray.Length;
            Gizmos.DrawLine(ray.Origin, endPoint);
            Gizmos.DrawSphere(endPoint, GizmoSphereRadius);
        }
    }

    private static Color GetGizmoColor(RayDebugData ray, bool hasValidHit)
    {
        if (!ray.Initialized)
        {
            return Color.gray;
        }

        if (!ray.HasHit)
        {
            return Color.red;
        }

        return hasValidHit ? Color.green : Color.yellow;
    }

    private readonly struct RayDebugData
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly float Length;
        public readonly bool HasHit;
        public readonly Vector3 HitPoint;
        public readonly bool Initialized;

        private RayDebugData(Vector3 origin, Vector3 direction, float length, bool hasHit, Vector3 hitPoint, bool initialized)
        {
            Origin = origin;
            Direction = direction;
            Length = length;
            HasHit = hasHit;
            HitPoint = hitPoint;
            Initialized = initialized;
        }

        public static RayDebugData Create(Vector3 origin, Vector3 direction, float length, bool hasHit, Vector3 hitPoint)
        {
            return new RayDebugData(origin, direction, length, hasHit, hitPoint, true);
        }
    }
}
