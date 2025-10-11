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
    
    [SerializeField] private Vector3 halfExtents = new(1, 1, 1);
    [SerializeField] private Vector3 origin;

    [Header("Shoot Checker")]
    
    [SerializeField] private LayerMask _aimCollisionMask = ~0; // configurá Ground/Environment
    [SerializeField, Range(0, 30)] private float _maxDistance;
    [SerializeField, Range(0, 30)] private float _launchSpeed;
    [SerializeField, Range(0, 30)] private int _simMaxSteps;
    [SerializeField, Range(0.005f, 0.25f)] private float _projectileRadius = 0.05f;
    [SerializeField, Range(0.005f, 0.05f)] private float _simDt = 0.02f; // 50 Hz

    // propiedad para que los States lean el mismo valor (exponen datos, no lógica)
    public float AttractMinDistance => _attractMinDistance;
    public float AttractMaxDistance => _attractMaxDistance;
    public AnimationCurve AttractSpeedCurve => _attractSpeedAC;
    public float AttractSpeedBase => _attractSpeedBase;

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
        Vector3 center = playerTf.position + origin;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, playerTf.rotation, _interactMask);

        float minDist = float.MaxValue;
        Rigidbody nearest = null;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Hook")) continue;
            Rigidbody rb = hit.attachedRigidbody;
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
    
// NUEVA FIRMA: devuelve bool + out point/normal
public bool TryGetAim(Transform playertf, out Vector3 hitPoint)
{
    hitPoint = default;

    // 1) Punto de salida del proyectil (boquilla / mano)
    Vector3 startPos = playertf.position + Vector3.up * 1.0f;

    // 2) Target “deseado” desde cámara (solo guía de dirección)
    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    Vector3 desired;
    if (Physics.Raycast(mouseRay, out RaycastHit camHit, 100f, ~0, QueryTriggerInteraction.Ignore))
        desired = camHit.point;
    else
        desired = mouseRay.GetPoint(_maxDistance);

    // 3) Dirección horizontal limitada por rango
    Vector3 toDesired = desired - startPos;
    Vector3 dirXZ = new Vector3(toDesired.x, 0f, toDesired.z);
    if (dirXZ.sqrMagnitude > 0.0001f)
        dirXZ = dirXZ.normalized;
    else
        dirXZ = playertf.forward;

    float g = 9.81f;
    float v = _launchSpeed;

    // 4) Ángulo de tiro (intento resolver a objetivo; si no, fijo 35°)
    float distance = new Vector2(toDesired.x, toDesired.z).magnitude;
    float height   = toDesired.y;
    float angleRad = 35f * Mathf.Deg2Rad;

    float underRoot = v*v*v*v - g * (g * distance * distance + 2f * height * v * v);
    if (underRoot >= 0f && distance > 0.01f)
    {
        underRoot = Mathf.Max(underRoot, 0f);
        float tanTheta = Mathf.Clamp((v*v - Mathf.Sqrt(underRoot)) / (g * distance), -10f, 10f);
        angleRad = Mathf.Atan(tanTheta);
    }

    // 5) Velocidad inicial (descompuesta)
    Vector3 vel = dirXZ * (v * Mathf.Cos(angleRad)) + Vector3.up * (v * Mathf.Sin(angleRad));

    // 6) Integración por pasos y detección de impacto
    Vector3 prev = startPos;
    float traveled = 0f;

    for (int i = 0; i < _simMaxSteps; i++)
    {
        // Avanzar física simple
        vel += Vector3.down * g * _simDt;
        Vector3 next = prev + vel * _simDt;

        // Colisión: SphereCast fino para no atravesar cosas delgadas
        bool hit;
        RaycastHit h;
        if (_projectileRadius > 0.005f)
            hit = Physics.SphereCast(prev, _projectileRadius, (next - prev).normalized,
                                     out h, (next - prev).magnitude, _aimCollisionMask,
                                     QueryTriggerInteraction.Ignore);
        else
            hit = Physics.Linecast(prev, next, out h, _aimCollisionMask, QueryTriggerInteraction.Ignore);

        if (hit)
        {
            hitPoint  = h.point;
            // Trazo de debug
            Debug.DrawLine(prev, h.point, Color.yellow);
            return true;
        }

        // Trazo de debug para seguir la curva
        Debug.DrawLine(prev, next, Color.yellow);

        traveled += (next - prev).magnitude;

        // 7) Cortes por rango: si excede 20u, no marcamos nada
        // (usa ambos: recorrido real y distancia desde el origen)
        if (traveled > _maxDistance || (next - startPos).magnitude > _maxDistance)
            break;

        prev = next;
    }

    // No golpeó nada en rango
    return false;
}

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        // SWING area (ya estaba)
        Transform tf = transform;
        Vector3 center = tf.position + origin;
        Quaternion rot = tf.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, _interactMask);
        bool anyHook = false;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Hook"))
            {
                anyHook = true;
                break;
            }
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