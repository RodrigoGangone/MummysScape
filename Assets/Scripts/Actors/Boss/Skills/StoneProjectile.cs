using System;
using System.Collections;
using static PauseUtils;
using UnityEngine;

/// <summary>
/// Mueve la piedra a lo largo de una Bézier cuadrática y detecta impacto.
/// - Utiliza un SphereCast unificado en Update para detectar paredes y al jugador.
/// - Empuja al Player al impactar (si tiene Rigidbody) solo en el plano XZ.
/// - Respeta ocultamiento: no golpea a través de paredes (wallMask).
/// - Stun simple: deshabilita el script Player por un tiempo y lo vuelve a habilitar.
/// </summary>
public class StoneProjectile : MonoBehaviour, IPausable
{
    [Header("Detección")]
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private LayerMask playerMask = ~0; // por defecto, todo

    [Header("Paredes / Ocultamiento")]
    [Tooltip("Capas consideradas pared/obstáculo. La piedra se detendrá aquí.")]
    [SerializeField] private LayerMask wallMask = 0;
    [Tooltip("Factor del radio usado en el SphereCast (<= hitRadius).")]
    [SerializeField, Range(0.1f, 1f)] private float sphereCastRadiusFactor = 0.9f;

    [Header("Empujón al Player")]
    [SerializeField, Min(0f)] private float pushForce = 12f;
    [SerializeField] private ForceMode pushForceMode = ForceMode.Impulse;

    [Header("Stun simple")]
    [SerializeField, Min(0.01f)] private float stunDuration = 1.0f;

    [Header("Fx/Visual")]
    [SerializeField] private GameObject view;
    [SerializeField] private GameObject fxImpact;

    private bool _paused;
    private event Action<Vector3> OnHit;
    // Bézier y tiempo
    private Vector3 p0, p1, p2;
    private float duration;
    private float t;

    // Contexto
    private IBossContext _ctx;

    // Estado
    private bool _hit;
    private Vector3 _lastPos;

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
    }

    void Update()
    {
        if (_paused || _hit) return;

        t += Time.deltaTime / duration;
        float u = Mathf.Clamp01(t);

        Vector3 currentPos = Bezier(p0, p1, p2, u);
        
        Vector3 delta = currentPos - _lastPos;
        float dist = delta.magnitude;

        if (dist > 0.001f)
        {
            float radius = Mathf.Max(0.01f, hitRadius * sphereCastRadiusFactor);
            LayerMask combinedMask = wallMask | playerMask;

            if (Physics.SphereCast(_lastPos, radius, delta.normalized, out var hitInfo, dist, combinedMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = hitInfo.point;

                int hitLayer = hitInfo.collider.gameObject.layer;

                if (IsInLayerMask(hitLayer, wallMask))
                {
                    OnHitWall(hitInfo);
                    return;
                }
                
                if (IsInLayerMask(hitLayer, playerMask))
                {
                    OnHitPlayer(hitInfo.collider);
                    return;
                }
            }
        }

        transform.position = currentPos;
        
        if (u < 0.995f)
        {
            Vector3 nextPos = Bezier(p0, p1, p2, Mathf.Min(1f, u + 0.02f));
            Vector3 lookDir = nextPos - currentPos;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        if (u >= 1f)
        {
            Destroy(gameObject, 0.05f);
        }

        _lastPos = currentPos;
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
        
        OnHit?.Invoke(other.transform.position);

        // var player = other.GetComponentInParent<Player>();
        var hitRb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();

        if (hitRb != null)
        {
            Vector3 targetCenter = GetTargetCenter(other);
            Vector3 impactDir = targetCenter - transform.position;

            // <<< CAMBIO CLAVE: EMPUJE SOLO EN XZ >>>
            // 1. Creamos un vector de empuje "plano" anulando la componente Y.
            Vector3 pushDir = new Vector3(impactDir.x, 0f, impactDir.z);

            // 2. Nos aseguramos de que haya una dirección horizontal antes de aplicar la fuerza.
            //    Esto evita un error si el impacto es perfectamente vertical.
            if (pushDir.sqrMagnitude > 0.001f)
            {
                // 3. Normalizamos el vector plano y aplicamos la fuerza.
                hitRb.AddForce(pushDir.normalized * pushForce, pushForceMode);
            }
        }
        //
        // if (player != null)
        // {
        //     StartCoroutine(SimpleStun(player, stunDuration));
        // }

        Debug.Log("[BS_Stone] Impacto con Player (stun simple y empuje XZ aplicado).");
        if(view != null) view.SetActive(false);
    }

    void OnHitWall(RaycastHit hit)
    {
        if (_hit) return;
        _hit = true;
        
        OnHit?.Invoke(hit.transform.position);
        
        Debug.Log($"[BS_Stone] Impacto con pared en '{hit.collider.name}'.");
        Destroy(gameObject);
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

    // private IEnumerator SimpleStun(Player player, float seconds)
    // {
    //     player.enabled = false;
    //     yield return WaitForSecondsPausable(seconds, () => _paused);
    //     player.enabled = true;
    //     Destroy(gameObject);
    // }

    void SpawnFX(Vector3 pos) => Instantiate(fxImpact, pos, Quaternion.identity);
    
    public void OnPauseChanged(bool paused) => _paused = paused;

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        OnHit += SpawnFX;
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        OnHit -= SpawnFX;
    }
}