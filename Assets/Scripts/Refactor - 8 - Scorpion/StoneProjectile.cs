using System.Collections;
using UnityEngine;

/// <summary>
/// Mueve la piedra a lo largo de una Bézier cuadrática y detecta impacto.
/// - Empuja al Player al impactar (si tiene Rigidbody).
/// - Respeta ocultamiento: no golpea a través de paredes (wallMask).
/// - Evita atravesar paredes finas con un SphereCast entre frames.
/// - Stun simple: deshabilita el script Player por un tiempo y lo vuelve a habilitar.
/// </summary>
public class StoneProjectile : MonoBehaviour
{
    [Header("Detección")] [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private LayerMask playerMask = ~0; // por defecto, todo

    [Header("Paredes / Ocultamiento")]
    [Tooltip("Capas consideradas pared/obstáculo. La piedra impactará aquí y NO pegará al player detrás.")]
    [SerializeField] private LayerMask wallMask = 0;

    [Tooltip("Ajuste pequeño para el raycast LOS; evita contar el punto exacto del centro del player.")]
    [SerializeField, Min(0f)] private float wallRayPadding = 0.02f;

    [Tooltip("Usar SphereCast entre frames para no 'pasar a través' de paredes finas.")] [SerializeField]
    private bool useSphereCastBetweenFrames = true;

    [Tooltip("Factor del radio usado en el SphereCast (<= hitRadius).")] [SerializeField, Range(0.1f, 1f)]
    private float sphereCastRadiusFactor = 0.9f;

    [Header("Empujón al Player")] [SerializeField, Min(0f)]
    private float pushForce = 12f;

    [SerializeField, Range(0f, 1f)] private float upwardFactor = 0.2f;
    [SerializeField] private ForceMode pushForceMode = ForceMode.Impulse;

    [Header("Stun simple")] [SerializeField, Min(0.01f)]
    private float stunDuration = 1.0f; // ⬅️ tiempo de "KO"

    [SerializeField] private GameObject view;
    
    // Bézier y tiempo
    Vector3 p0, p1, p2;
    float duration;
    float t;

    // Contexto (si lo usás)
    IBossContext _ctx;

    // Estado
    bool _hit;
    Vector3 _lastPos;

    public void Initialize(Vector3 start, Vector3 control, Vector3 end, float dur, IBossContext bossCtx)
    {
        p0 = start;
        p1 = control;
        p2 = end;
        duration = Mathf.Max(0.05f, dur);
        t = 0f;
        _ctx = bossCtx;

        transform.position = p0;
        _lastPos = p0;

        // Asegurar trigger simple si no hay collider
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<SphereCollider>();
            if (col is SphereCollider sc) sc.radius = hitRadius * 0.5f;
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (_hit) return;

        t += Time.deltaTime / duration;
        float u = Mathf.Clamp01(t);

        // Posición actual
        Vector3 pos = Bezier(p0, p1, p2, u);

        // 1) Anti-túnel contra paredes: SphereCast entre _lastPos -> pos
        if (useSphereCastBetweenFrames)
        {
            Vector3 delta = pos - _lastPos;
            float dist = delta.magnitude;
            if (dist > 0.0001f)
            {
                float radius = Mathf.Max(0.01f, hitRadius * sphereCastRadiusFactor);
                if (Physics.SphereCast(_lastPos, radius, delta.normalized, out var wallHit, dist, wallMask,
                        QueryTriggerInteraction.Ignore))
                {
                    transform.position = wallHit.point;
                    OnHitWall(wallHit);
                    return;
                }
            }
        }

        transform.position = pos;

        // Orientación "natural"
        if (u < 0.995f)
        {
            float u2 = Mathf.Min(1f, u + 0.02f);
            Vector3 next = Bezier(p0, p1, p2, u2);
            Vector3 dir = next - pos;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        // 2) Detección de Player por OverlapSphere (con LOS contra pared)
        var hits = Physics.OverlapSphere(pos, hitRadius, playerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            var playerComp = hits[i].GetComponentInParent<Player>();
            if (playerComp == null) continue;

            Vector3 targetCenter = GetTargetCenter(hits[i]);
            // Si hay pared entre la piedra y el player, impactar en la pared (no al player)
            if (TryWallHitAlong(pos, targetCenter)) return;

            OnHitPlayer(hits[i]); // ⬅️ hará el stun simple
            break;
        }

        // 3) Fin del vuelo
        if (u >= 1f && !_hit)
            Destroy(gameObject, 0.05f);

        _lastPos = pos;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hit) return;

        // Si es pared (por capa), impactar en la pared
        if (IsInLayerMask(other.gameObject.layer, wallMask))
        {
            Vector3 point = other.ClosestPoint(transform.position);
            OnHitWall(point);
            return;
        }

        // Si es player, validar LOS primero y luego stun simple
        if (other.GetComponentInParent<Player>() != null)
        {
            Vector3 pos = transform.position;
            Vector3 targetCenter = GetTargetCenter(other);
            if (TryWallHitAlong(pos, targetCenter)) return;

            OnHitPlayer(other);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }

    // ================== Helpers de impacto ==================

    void OnHitPlayer(Collider other)
    {
        if (_hit) return;
        _hit = true;

        var player = other.GetComponentInParent<Player>();
        var hitRb = other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>();

        Vector3 targetCenter = GetTargetCenter(other);
        Vector3 dir = (targetCenter - transform.position).normalized;
        Vector3 pushDir = Vector3.Normalize(dir + Vector3.up * upwardFactor);

        // Empuje solo si hay Rigidbody
        if (hitRb != null)
        {
            hitRb.AddForce(pushDir * pushForce, pushForceMode);
        }

        // ⬇️ STUN SIMPLE: deshabilita el script Player y lo re-habilita luego
        if (player != null)
        {
            StartCoroutine(SimpleStun(player, stunDuration));
        }

        Debug.Log("[BS_Stone] Impacto con Player (stun simple aplicado).");
        
        view.SetActive(false);
    }

    void OnHitWall(RaycastHit hit)
    {
        if (_hit) return;
        _hit = true;
        Debug.Log("[BS_Stone] Impacto con pared (LOS bloqueado).");
        Destroy(gameObject);
    }

    void OnHitWall(Vector3 point)
    {
        if (_hit) return;
        _hit = true;
        Debug.Log("[BS_Stone] Impacto con pared (trigger/closestPoint).");
        Destroy(gameObject);
    }

    bool TryWallHitAlong(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.0001f) return false;

        if (Physics.Raycast(from, dir.normalized, out var hit, Mathf.Max(0f, dist - wallRayPadding), wallMask,
                QueryTriggerInteraction.Ignore))
        {
            OnHitWall(hit);
            return true;
        }

        return false;
    }

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    static Vector3 GetTargetCenter(Collider col)
    {
        var rb = col.attachedRigidbody ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody>();
        return rb != null ? rb.worldCenterOfMass : col.bounds.center;
    }

    // ================== STUN SIMPLE ==================
    IEnumerator SimpleStun(Player player, float seconds)
    {
        if (player == null) yield break;

        // Deshabilitar el script Player
        player.enabled = false;
        yield return new WaitForSeconds(seconds);

        // Rehabilitar (si sigue existiendo)
        if (player != null) player.enabled = true;
        Destroy(gameObject);
    }
}