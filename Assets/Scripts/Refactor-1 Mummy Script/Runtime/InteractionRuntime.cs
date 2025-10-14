using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

//// <summary>
//// InteractionRuntime
//// - PushChecker: 2 rayos frontales (ya existente).
//// - SwingChecker: OverlapBox (ya existente).
//// - AttractChecker: 1 raycast tipo Line-of-Sight a Y=1, rango [min,max], layer Interactable,
////   requiere componente BoxPushAttract y que la caja esté grounded.
////   Gizmos: VERDE si válido, ROJO si no.
//// </summary>
[DisallowMultipleComponent]
public sealed class InteractionRuntime : MonoBehaviour
{
    [Header("Push Checker")]
    
    [SerializeField] private float _heightY = 1.0f;
    [SerializeField] private float _distance = 1.0f;
    [SerializeField] private float _separation = 0.5f;
    [SerializeField] private LayerMask _interactMask;

    [Header("Attract Checker")] 
    
    [SerializeField] private float _attractMinDistance = 1.0f;
    [SerializeField] private float _attractMaxDistance = 5.0f;
    [SerializeField] private AnimationCurve _attractSpeedAC = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField, Min(0f)] private float _attractSpeedBase = 1.0f;

    [Header("Swing Checker")] 
    
    [SerializeField] private Vector3 halfExtents = new(5, 5, 7);
    [Tooltip("Offset LOCAL respecto al pivot del player (se transforma con su rotación).")]
    [SerializeField] private Vector3 origin = new(0, 5, 7);

    [Header("Shoot Checker")]
    
    [SerializeField] private LayerMask _aimCollisionMask = ~0; // configurá Ground/Environment
    [SerializeField, Range(0, 30)] private float _maxDistance;
    [SerializeField, Range(0, 30)] private float _arcHeight;
    [SerializeField, Range(0, 200)] private int _simMaxSteps;
    [SerializeField] private GameObject projectilePrefab;
    
    // propiedad para que los States lean el mismo valor (exponen datos, no lógica)
    public float AttractMinDistance => _attractMinDistance;
    public float AttractMaxDistance => _attractMaxDistance;
    public AnimationCurve AttractSpeedCurve => _attractSpeedAC;
    public float AttractSpeedBase => _attractSpeedBase;
    
    public GameObject ProjectilePrefab => projectilePrefab;

    [Header("Debug")] [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _hitColor = new(0.2f, 1f, 0.2f, 0.9f);
    [SerializeField] private Color _missColor = new(1f, 0.2f, 0.2f, 0.9f);

    // cache push gizmos
    private Vector3 _oLeft, _oRight, _dLeft, _dRight;
    private bool _leftHitInteract, _rightHitInteract;
    private Vector3 _leftHitPoint, _rightHitPoint;

    // cache attract gizmos
    private Vector3 _aOrigin, _aEnd, _aHitPoint;
    private bool _aEligible;


    // -------------------- PUSH --------------------
    public bool TryGetPushTarget(Transform playerTf, out BoxPushAttract target, out RaycastHit hitLeft,
        out RaycastHit hitRight)
    {
        target = null;
        hitLeft = default;
        hitRight = default;

        var fwd = playerTf.forward;
        var right = playerTf.right;
        var center = playerTf.position + Vector3.up * _heightY;
        float half = _separation * 0.5f;

        _oLeft = center - right * half;
        _oRight = center + right * half;
        _dLeft = _dRight = fwd;

        bool lHit = Physics.Raycast(_oLeft, fwd, out hitLeft, _distance, _interactMask, QueryTriggerInteraction.Ignore);
        bool rHit = Physics.Raycast(_oRight, fwd, out hitRight, _distance, _interactMask,
            QueryTriggerInteraction.Ignore);

        _leftHitInteract = lHit;
        _rightHitInteract = rHit;
        _leftHitPoint = lHit ? hitLeft.point : _oLeft + fwd * _distance;
        _rightHitPoint = rHit ? hitRight.point : _oRight + fwd * _distance;

        if (!(lHit && rHit)) return false;

        Transform a = hitLeft.collider.transform.root;
        Transform b = hitRight.collider.transform.root;
        if (a != b) return false;

        target = a.GetComponentInChildren<BoxPushAttract>();
        if (target == null) return false;

        return target.IsGroundedForPushAttract();
    }

    // -------------------- SWING --------------------
    public bool TryGetSwingTarget(Transform playerTf, out Rigidbody target)
    {
        target = null;

        // centro local del player
        Vector3 center = playerTf.TransformPoint(origin);
        // usa rotacion del player
        Collider[] hits = Physics.OverlapBox(center, halfExtents, playerTf.rotation, _interactMask);

        float minDist = float.MaxValue;
        Rigidbody nearest = null;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Hook")) continue;
            var rb = hit.attachedRigidbody;
            if (rb == null) continue;

            float dist = Vector3.Distance(playerTf.position, rb.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = rb;
            }
        }

        if (nearest == null) return false;
        target = nearest;
        return true;
    }

    // -------------------- ATTRACT --------------------
    /// <summary>
    /// Raycast tipo LOS frente al player (Y=1) en rango [min,max] contra Interactable.
    /// Requiere BoxPushAttract y que la caja esté grounded para atraer.
    /// </summary>
    public bool TryGetAttractTarget(Transform playerTf, out BoxPushAttract target)
    {
        target = null;

        Vector3 fwd = playerTf.forward;
        _aOrigin = playerTf.position + Vector3.up * _heightY;
        _aEnd = _aOrigin + fwd * _attractMaxDistance;

        bool hit = Physics.Raycast(_aOrigin, fwd, out RaycastHit h, _attractMaxDistance, _interactMask,
            QueryTriggerInteraction.Ignore);
        _aHitPoint = hit ? h.point : _aEnd;

        if (!hit)
        {
            _aEligible = false;
            return false;
        }

        const float EPS = 0.001f;
        // 🔴 No permitir adquisición si está en la distancia mínima o por debajo
        if (h.distance <= (_attractMinDistance + EPS))
        {
            _aEligible = false;
            return false;
        }

        Transform root = h.collider.transform.root;
        target = root.GetComponentInChildren<BoxPushAttract>();
        if (target == null)
        {
            _aEligible = false;
            return false;
        }

        _aEligible = target.IsGroundedForPushAttract();
        return _aEligible;
    }

    // -------------------- SHOOT --------------------
    
    public bool TryGetAim(Transform playertf, out Vector3 hitPoint)
    {
        hitPoint = default;
        SimpleShootData.Path = null; // limpiar siempre

        // 1) Origen
        Vector3 start = playertf.position + Vector3.up * 1.0f;

        // 2) Punto “deseado” desde cámara (guía)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 desired = Physics.Raycast(ray, out RaycastHit camHit, 100f, ~0, QueryTriggerInteraction.Ignore)
            ? camHit.point
            : ray.GetPoint(_maxDistance);

        // 3) Dirección horizontal
        Vector3 toDesired = desired - start;
        Vector3 dirXZ = new Vector3(toDesired.x, 0f, toDesired.z);
        float distXZ = dirXZ.magnitude;
        if (distXZ > 1e-3f) dirXZ /= distXZ; else { dirXZ = playertf.forward; distXZ = 1f; }

        // 4) Parámetros del arco “plantilla”
        float L      = Mathf.Min(_maxDistance, distXZ);
        float height = toDesired.y;
        int   steps  = Mathf.Max(6, _simMaxSteps);

        // 5) Muestreo y colisión por segmentos + bake del path
        var points = new List<Vector3>(steps + 1);
        Vector3 prev = start;
        points.Add(prev);

        for (int i = 1; i <= steps; i++)
        {
            float s = i / (float)steps;                 // 0..1
            Vector3 flat = start + dirXZ * (L * s);     // avance horizontal
            float y = Mathf.Lerp(0f, height, s) + 4f * _arcHeight * s * (1f - s); // apex en s=0.5
            Vector3 p = new Vector3(flat.x, start.y + y, flat.z);

            if (Physics.Linecast(prev, p, out RaycastHit h, _aimCollisionMask, QueryTriggerInteraction.Ignore))
            {
                hitPoint = h.point;
                points.Add(hitPoint);          
                SimpleShootData.Path = points; 
                return true;
            }

            points.Add(p);
            prev = p;
        }

        // No golpeó nada => no hay Aim válido
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        Transform tf = transform;
        Vector3 center = tf.TransformPoint(origin);
        Quaternion rot = tf.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, _interactMask);
        bool anyHook = false;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Hook")) { anyHook = true; break; }
        }

        Gizmos.color = anyHook ? _hitColor : _missColor;
        Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        // PUSH rays
        Gizmos.color = _leftHitInteract ? _hitColor : _missColor;
        Gizmos.DrawLine(_oLeft, _oLeft + _dLeft * _distance);
        Gizmos.DrawSphere(_leftHitPoint, 0.03f);

        Gizmos.color = _rightHitInteract ? _hitColor : _missColor;
        Gizmos.DrawLine(_oRight, _oRight + _dRight * _distance);
        Gizmos.DrawSphere(_rightHitPoint, 0.03f);

        // ATTRACT LOS
        Gizmos.color = _aEligible ? _hitColor : _missColor;
        Gizmos.DrawLine(_aOrigin, _aEnd);
        Gizmos.DrawSphere(_aHitPoint, 0.03f);

        // marca de distancia mínima
        var minMark = _aOrigin + ((_aEnd - _aOrigin).normalized * _attractMinDistance);
        Gizmos.DrawWireSphere(minMark, 0.05f);
    }
}