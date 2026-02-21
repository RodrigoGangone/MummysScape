using System;
using UnityEngine;

/// <summary>
/// Lógica de Proyectil: Controla el desplazamiento por curva Bézier, detecta impactos 
/// mediante SphereCast y aplica fuerzas físicas (push) al Player si es alcanzado.
///
/// TODO: Es una implementacion vieja para lanzar el proyectil, se guarda dentro 
/// TODO: del proyecto para reutilizarlo en proximos enemigos
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
    
    [Header("Fx/Visual")]
    [SerializeField] private GameObject view;
    [SerializeField] private GameObject fxImpact;

    private bool _paused;
    private event Action<Vector3> OnHit;
    private Vector3 p0, p1, p2;
    private float duration;
    private float t;

    private IBossContext _ctx;

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
    
    void OnHitPlayer(Collider other)
    {
        if (_hit) return;
        _hit = true;
        
        OnHit?.Invoke(other.transform.position);

        var hitRb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();

        if (hitRb != null)
        {
            Vector3 targetCenter = GetTargetCenter(other);
            Vector3 impactDir = targetCenter - transform.position;

            Vector3 pushDir = new Vector3(impactDir.x, 0f, impactDir.z);

            if (pushDir.sqrMagnitude > 0.001f)
                hitRb.AddForce(pushDir.normalized * pushForce, pushForceMode);
        }
        
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