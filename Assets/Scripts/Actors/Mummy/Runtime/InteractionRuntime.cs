using System.Collections.Generic;
using UnityEngine;
using static Layers;
using static Tags;

/// <summary> 
/// Centro de Interacciones: Gestiona múltiples mecánicas de detección como empuje (Push), atracción (Attract), 
/// balanceo (Swing) y apuntado (Aim). Utiliza una combinación de Raycasts, OverlapBox 
/// y cálculos de trayectoria parabólica, incorporando lógica de oclusión para evitar interacciones a través de paredes.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionRuntime : MonoBehaviour
{
    [Header("Push Checker")] [SerializeField]
    private float _heightY = 1.0f;

    [SerializeField] private float _distance = 1.0f;
    [SerializeField] private float _separation = 0.5f;
    [SerializeField] private LayerMask _interactMask;

    [Header("Attract Checker")] [SerializeField]
    private float _attractMinDistance = 1.0f;

    [SerializeField] private float _attractMaxDistance = 5.0f;
    [SerializeField] private AnimationCurve _attractSpeedAC = AnimationCurve.Linear(0, 1, 1, 1);

    [SerializeField, Min(0f)] private float _attractSpeedBase = 1.0f;

    [Header("Swing Checker")] [SerializeField]
    private Vector3 halfExtents = new(5, 5, 7);

    [Tooltip("Offset LOCAL respecto al pivot del player (se transforma con su rotación).")] [SerializeField]
    private Vector3 origin = new(0, 5, 7);

    [Header("Shoot Checker")] [SerializeField]
    private LayerMask _aimCollisionMask = ~0;

    [SerializeField] private Transform _shootOriginTransform;
    [SerializeField, Range(1, 30)] private float _aimMaxDistance;
    [SerializeField, Range(0, 5)] private float _aimMinDistance;
    [SerializeField, Range(-5, 5)] private float _maxAimHeight;
    [SerializeField, Range(0, 30)] private float _arcHeight;
    [SerializeField, Range(0.01f, 0.5f)] private float _arcRadius = 0.1f;
    [SerializeField, Range(0, 200)] private int _simMaxSteps;

    [Header("Quick Travel")] [SerializeField]
    private float radiusTiny;

    [Header("Smash")] [SerializeField] public float smashRange = 3f;
    [SerializeField] public LayerMask smashLayer;

    // propiedad para que los States lean el mismo valor (exponen datos, no lógica)
    public float AttractMinDistance => _attractMinDistance;
    public float AttractMaxDistance => _attractMaxDistance;
    public AnimationCurve AttractSpeedCurve => _attractSpeedAC;
    public float AttractSpeedBase => _attractSpeedBase;
    public Transform ShootOrigin => _shootOriginTransform;
    public float AimMaxDistance => _aimMaxDistance;
    public float AimMinDistance => _aimMinDistance;
    public float AimMaxHeight => _maxAimHeight;
    public bool IsAimValid { get; private set; }


    private Vector3 _oLeft, _oRight, _dLeft, _dRight;
    private bool _leftHitInteract, _rightHitInteract;
    private Vector3 _leftHitPoint, _rightHitPoint;

    private Vector3 _aOrigin, _aEnd, _aHitPoint;
    private bool _aEligible;

    // -------------------- PUSH --------------------

    /// <summary> 
    /// Detecta objetivos de empuje mediante rayos frontales duales, validando que el objeto 
    /// sea una caja interactuable válida y esté apoyada en el suelo. 
    /// </summary>
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

    /// <summary> 
    /// Busca puntos de balanceo (Hooks) mediante un área de colisión local, verificando que 
    /// no existan obstáculos (paredes) que bloqueen la línea de visión hacia el objetivo. 
    /// </summary>
    public bool TryGetSwingTarget(Transform playerTf, out Rigidbody target)
    {
        target = null;

        Vector3 center = playerTf.TransformPoint(origin);
        Collider[] hits = Physics.OverlapBox(center, halfExtents, playerTf.rotation, _interactMask);

        float minDist = float.MaxValue;
        Rigidbody nearest = null;

        int wallMask = LayerMask.GetMask(WALL_LAYER);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(HOOK_TAG)) continue;
            var rb = hit.attachedRigidbody;
            if (rb == null) continue;

            float dist = Vector3.Distance(playerTf.position, rb.position);

            if (dist >= minDist) continue;

            Vector3 direction = (rb.position - playerTf.position).normalized;

            if (Physics.Raycast(playerTf.position, direction, dist, wallMask))
            {
                continue;
            }

            minDist = dist;
            nearest = rb;
        }

        if (nearest == null) return false;
        target = nearest;
        return true;
    }

    // -------------------- ATTRACT --------------------

    /// <summary> 
    /// Identifica cajas para atraer a distancia mediante un rayo de visión, validando que 
    /// se encuentre dentro del rango permitido y en contacto con la superficie. 
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

    // -------------------- AIM --------------------

    /// <summary> 
    /// Calcula una trayectoria parabólica basada en la posición del cursor, simulando el arco 
    /// paso a paso para detectar el punto exacto de impacto mediante SphereCast. 
    /// </summary>
public bool TryGetAim(Transform playertf, Vector2 aimScreenPosition, out Vector3 hitPoint, out Vector3 hitNormal)
{
    hitPoint = default;
    hitNormal = Vector3.up;
    SimpleShootData.Path = null;

    // El arco y la distancia SIEMPRE nacen desde el jugador/arma
    Vector3 origin = _shootOriginTransform != null
        ? _shootOriginTransform.position
        : playertf.position + Vector3.up * 1.0f;
        
    Ray ray = Camera.main.ScreenPointToRay(aimScreenPosition);
    Vector3 desired =
        Physics.Raycast(ray, out RaycastHit camHit, 200f, _aimCollisionMask, QueryTriggerInteraction.Ignore)
            ? camHit.point
            : ray.GetPoint(50f);

    Vector3 toDesired = desired - origin;
    Vector3 dirXZ = new Vector3(toDesired.x, 0f, toDesired.z);
    float rawDistXZ = dirXZ.magnitude;
    
    // Si el raycast choca con el propio jugador al iniciar, forzamos la dirección hacia adelante
    if (rawDistXZ < 0.5f) 
    {
        dirXZ = playertf.forward;
    }
    else 
    {
        dirXZ.Normalize();
    }

    // --- NUEVO: Cálculo del Punto Final ---
    // Empujamos el OBJETIVO hacia adelante. Si rawDistXZ es menor que la dona, 
    // lo forzamos a ser la distancia mínima + 0.1f de offset.
    float targetDistXZ = Mathf.Max(rawDistXZ, _aimMinDistance + 0.1f);

    bool isValid = true;

    // Solo invalidamos si se pasa de la distancia máxima
    if (targetDistXZ > _aimMaxDistance)
        isValid = false;

    if (toDesired.y > _maxAimHeight)
        isValid = false;

    // El largo total del arco ahora es nuestro targetDistXZ empujado
    float L = targetDistXZ; 
    float height = toDesired.y; 
    
    int steps = Mathf.Max(6, _simMaxSteps);
    var points = new List<Vector3>(steps + 1);
    
    // El arco VUELVE a iniciar desde el origen (el jugador)
    Vector3 prev = origin;
    points.Add(prev);
    
    float maxWorldHeight = origin.y + _maxAimHeight;
    bool collisionFound = false;

    for (int i = 1; i <= steps; i++)
    {
        float s = i / (float)steps;
        
        // Calculamos la parábola normal desde el jugador hasta el targetDistXZ
        Vector3 flat = origin + dirXZ * (L * s);
        float y = Mathf.Lerp(0f, height, s) + 4f * _arcHeight * s * (1f - s);
        Vector3 p = new Vector3(flat.x, origin.y + y, flat.z);

        if (p.y > maxWorldHeight) isValid = false;

        Vector3 dir = p - prev;
        float dist = dir.magnitude;
        if (dist > 0.001f)
        {
            if (Physics.SphereCast(prev, _arcRadius, dir, out RaycastHit h, dist, _aimCollisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 hitVector = h.point - origin;
                float hitDistXZ = new Vector3(hitVector.x, 0f, hitVector.z).magnitude;
                
                // Si la parábola choca con algo (como una pared) DENTRO de la zona mínima, es inválido (rojo)
                if (hitDistXZ > _aimMaxDistance || hitDistXZ < _aimMinDistance) 
                    isValid = false;

                hitPoint = h.point;
                hitNormal = h.normal;
                points.Add(hitPoint);
                collisionFound = true;
                break;
            }
        }

        points.Add(p);
        prev = p;
    }

    SimpleShootData.Path = points;

    if (SimpleShootData.Path == null || SimpleShootData.Path.Count < 2)
    {
        SimpleShootData.Path = new List<Vector3> { origin, origin + dirXZ * 0.1f };
    }

    if (!collisionFound)
    {
        if (targetDistXZ > _aimMaxDistance) 
            isValid = false;
            
        hitPoint = points[points.Count - 1];
    }

    IsAimValid = isValid;

    return isValid;
}
    /// <summary> 
    /// Localiza el componente de transporte (HippoTravel) más cercano al jugador dentro de 
    /// un radio de búsqueda reducido. 
    /// </summary>
    public bool TryGetQuickTravel(Transform playerTransform, out HippoTravel portal)
    {
        portal = null;

        Vector3 center = playerTransform.position;

        Collider[] hits = Physics.OverlapSphere(center, radiusTiny);

        if (hits == null || hits.Length == 0)
            return false;

        var bestSqr = float.PositiveInfinity;
        HippoTravel best = null;

        foreach (var col in hits)
        {
            if (!col) continue;

            var p = col.GetComponentInParent<HippoTravel>();
            if (p == null || !p.isActiveAndEnabled) continue;

            var sqr = (p.transform.position - center).sqrMagnitude;

            if (!(sqr < bestSqr)) continue;

            bestSqr = sqr;
            best = p;
        }

        portal = best;
        return portal != null;
    }

     #region Gizmos

     [Header("Debug")] [SerializeField] private bool _drawGizmos = true;
     [SerializeField] private Color _hitColor = new(0.2f, 1f, 0.2f, 0.9f);
     [SerializeField] private Color _missColor = new(1f, 0.2f, 0.2f, 0.9f);

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
     //Gizmos.color = _aEligible ? _hitColor : _missColor;
     //Gizmos.DrawLine(_aOrigin, _aEnd);
     //Gizmos.DrawSphere(_aHitPoint, 0.03f);
     //
     //var minMark = _aOrigin + ((_aEnd - _aOrigin).normalized * _attractMinDistance);
     //Gizmos.DrawWireSphere(minMark, 0.05f);

     //// SWING
     //Transform tf = transform;
     //Vector3 swingCenter = tf.TransformPoint(origin);
     //Quaternion swingRot = tf.rotation;
     //
     //Collider[] swingHits = Physics.OverlapBox(swingCenter, halfExtents, swingRot, _interactMask);
     //bool anyHook = false;
     //
     //foreach (var hit in swingHits)
     //{
     //    if (!hit) continue;
     //    if (hit.CompareTag("Hook"))
     //    {
     //        anyHook = true;
     //        break;
     //    }
     //}
//
     //Gizmos.color = anyHook ? _hitColor : _missColor;
     //Matrix4x4 oldMatrix = Gizmos.matrix;
     //Gizmos.matrix = Matrix4x4.TRS(swingCenter, swingRot, Vector3.one);
     //Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
     //Gizmos.matrix = oldMatrix;
//
     //// AIM
     //if (SimpleShootData.Path != null && SimpleShootData.Path.Count > 1)
     //{
     //    Gizmos.color = _hitColor;
     //    for (int i = 0; i < SimpleShootData.Path.Count - 1; i++)
     //    {
     //        Gizmos.DrawLine(SimpleShootData.Path[i], SimpleShootData.Path[i + 1]);
     //    }
//
     //    Vector3 impactPoint = SimpleShootData.Path[SimpleShootData.Path.Count - 1];
     //    Gizmos.DrawSphere(impactPoint, 0.25f);
     //}
//
     //// SMASH
     //bool smashHit = Physics.CheckSphere(transform.position, smashRange, smashLayer);
     //Gizmos.color = smashHit ? _hitColor : new Color(1f, 1f, 0f, 0.5f);
     //Gizmos.DrawWireSphere(transform.position, smashRange);
 }

     #endregion
}