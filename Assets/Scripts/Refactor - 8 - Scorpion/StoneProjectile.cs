using UnityEngine;

/// <summary>
/// Mueve la piedra a lo largo de una Bézier cuadrática y detecta impacto con el Player.
/// </summary>
public class StoneProjectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private LayerMask playerMask = ~0; // por defecto, todo

    Vector3 p0, p1, p2;
    float duration;
    float t;
    IBossContext _ctx;

    bool _hit;

    public void Initialize(Vector3 start, Vector3 control, Vector3 end, float dur, IBossContext bossCtx)
    {
        p0 = start; p1 = control; p2 = end;
        duration = Mathf.Max(0.05f, dur);
        t = 0f;
        _ctx = bossCtx;

        // Si no hay collider, agregamos uno simple para OnTriggerEnter
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<SphereCollider>();
            var sc = col as SphereCollider;
            if (sc != null) sc.radius = hitRadius * 0.5f;
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (_hit) return;

        t += Time.deltaTime / duration;
        float u = Mathf.Clamp01(t);

        // Bézier cuadrática: B(u) = (1-u)^2 P0 + 2(1-u)u P1 + u^2 P2
        Vector3 pos = (1 - u) * (1 - u) * p0 + 2f * (1 - u) * u * p1 + u * u * p2;
        transform.position = pos;

        // Orientación hacia el siguiente punto para que se vea "natural"
        if (u < 0.995f)
        {
            float u2 = Mathf.Min(1f, u + 0.02f);
            Vector3 next = (1 - u2) * (1 - u2) * p0 + 2f * (1 - u2) * u2 * p1 + u2 * u2 * p2;
            Vector3 dir = next - pos;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        // Chequeo manual por si el prefab trae collider distinto o no detecta
        var hits = Physics.OverlapSphere(pos, hitRadius, playerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponentInParent<Player>() != null)
            {
                OnHitPlayer();
                break;
            }
        }

        // Fin del vuelo
        if (u >= 1f && !_hit)
            Destroy(gameObject, 0.05f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hit) return;
        if (other.GetComponentInParent<Player>() != null)
            OnHitPlayer();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }

    void OnHitPlayer()
    {
        _hit = true;
        Debug.Log("[BS_Stone] ¡Impacto con el Player!");
        Destroy(gameObject);
    }
}
