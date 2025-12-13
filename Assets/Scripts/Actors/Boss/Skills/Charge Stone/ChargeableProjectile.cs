using System.Collections;
using UnityEngine;
using static PauseUtils; // Asumiendo que IsInLayerMask está aquí

/// <summary>
/// Un proyectil que primero se carga visualmente y luego se lanza.
/// Es un solo objeto/prefab.
/// Fase 1 (Charge): El script se añade, pero _isLaunched es false. Solo muestra FX.
/// Fase 2 (Launch): Se llama a Launch(), _isLaunched se vuelve true y empieza a moverse.
/// </summary>
public class ChargeableProjectile : MonoBehaviour, IPausable
{
    [Header("Detección")] [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private LayerMask playerMask = ~0;
    [SerializeField] private LayerMask wallMask = 0;

    [Header("Empujón al Player")] [SerializeField, Min(0f)]
    private float pushForce = 12f;

    [SerializeField] private ForceMode pushForceMode = ForceMode.Impulse;

    [Header("Stun simple")] [SerializeField, Min(0.01f)]
    private float stunDuration = 1.0f;


    [Header("Movimiento")] [SerializeField]
    private float speed = 20f;

    [SerializeField] private float lifetime = 5f;

    private bool _isLaunched;
    private bool _hit;
    private bool _paused;
    private Vector3 _direction;
    private Vector3 _lastPos;
    private IBossContext _ctx;

    // El BossAnimHandler llama a esto al instanciarlo (en AE_Primary_FX)
    public void Initialize(IBossContext ctx)
    {
        _ctx = ctx;
        _isLaunched = false;
        _hit = false;
        _lastPos = transform.position;
        // La partícula de "carga" debería empezar a reproducirse automáticamente al instanciarse.
    }

    // El BossAnimHandler (via el Skill SO) llama a esto (en AE_Primary_Launch)
    public void Launch()
    {
        if (_ctx == null || _ctx.Player == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. Apuntar al jugador en el momento del lanzamiento
        Vector3 targetPos = _ctx.Player.Tf.position + Vector3.up * 1.0f; // Ajuste de altura
        _direction = (targetPos - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(_direction);

        // 2. Activar movimiento y autodestrucción
        _isLaunched = true;
        StartCoroutine(Lifetime(lifetime));
    }

    void Update()
    {
        // No hacer nada si está pausado, si ya golpeó, o si aún no fue lanzado
        if (_paused || _hit || !_isLaunched) return;

        float distance = speed * Time.deltaTime;
        Vector3 delta = _direction * distance;
        LayerMask combinedMask = wallMask | playerMask;

        if (Physics.SphereCast(_lastPos, hitRadius, _direction, out var hitInfo, distance, combinedMask,
                QueryTriggerInteraction.Ignore))
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
        else
        {
            transform.position += delta;
        }

        _lastPos = transform.position;
    }

    // --- Corutinas y Eventos de Impacto ---

    private IEnumerator Lifetime(float seconds)
    {
        yield return WaitForSecondsPausable(seconds, () => _paused);

        Destroy(gameObject);
    }

    void OnHitPlayer(Collider other)
    {
        if (_hit) return;
        _hit = true;
        
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
    }

    void OnHitWall(RaycastHit hit)
    {
        if (_hit) return;
        _hit = true;
    }

    static Vector3 GetTargetCenter(Collider col)
    {
        var rb = col.attachedRigidbody ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody>();
        return rb != null ? rb.worldCenterOfMass : col.bounds.center;
    }

    // --- Pausa y Helpers ---
    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
}