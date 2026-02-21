using System.Collections;
using UnityEngine;
using static PauseUtils;

/// <summary> 
/// Lógica de Proyectil: Gestiona el desplazamiento lineal, la detección de colisiones con obstáculos 
/// y define los parámetros de impacto (stun y knockback) cuando el proyectil alcanza al Player.
/// </summary>

[RequireComponent(typeof(ParticleSystem))]
public class ChargeableProjectile : MonoBehaviour, IPausable, IImpactSource
{
    [Header("Detección")] 
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private LayerMask wallMask = 0; 

    [Header("Stun / Knockback Config")] 
    [SerializeField, Min(0.01f)] private float stunDuration = 1.0f;
    [SerializeField] private float knockBackDistance = 12f;

    [Header("Movimiento")] 
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;

    private bool _isLaunched;
    private bool _paused;
    private Vector3 _direction;
    private Vector3 _lastPos;
    private IBossContext _ctx;
    
    private Collider _myCollider;
    private ParticleSystem _particleSystem;
    private bool _hasImpacted = false; 
    
    private void Awake()
    {
        _myCollider = GetComponent<Collider>();
        _particleSystem = GetComponent<ParticleSystem>();

        var mainModule = _particleSystem.main;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
    }

    public KnockbackData GetKnockbackData(Vector3 victimPosition)
    {
        if (_hasImpacted) return new KnockbackData();

        if (Physics.Linecast(transform.position, victimPosition, wallMask))
        {
            BeginImpactSequence();
            return new KnockbackData(); 
        }
        
        BeginImpactSequence();

        Vector3 impactDir = (victimPosition - transform.position).normalized;
        Vector3 flatDir = new Vector3(impactDir.x, 0, impactDir.z).normalized;

        return new KnockbackData 
        {
            TargetPosition = victimPosition + (flatDir * knockBackDistance),
            Duration = stunDuration
        };
    }

    public void Initialize(IBossContext ctx)
    {
        _ctx = ctx;
        _isLaunched = false;
        _hasImpacted = false;
        _lastPos = transform.position;
        if (_myCollider != null) _myCollider.enabled = true;

        if (_particleSystem.isStopped) _particleSystem.Play();
    }

    public void Launch()
    {
        if (_ctx?.Player == null) { Destroy(gameObject); return; }

        Vector3 targetPos = _ctx.Player.Tf.position + Vector3.up * 1.0f;
        _direction = (targetPos - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(_direction);

        _isLaunched = true;
        
        StartCoroutine(LifetimeRoutine(lifetime));
    }

    private void Update()
    {
        if (_paused || !_isLaunched || _hasImpacted) return;

        float distance = speed * Time.deltaTime;

        if (Physics.SphereCast(_lastPos, hitRadius, _direction, out RaycastHit hitInfo, distance, wallMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hitInfo.point; 
            BeginImpactSequence();
            return;
        }

        transform.position += _direction * distance;
        _lastPos = transform.position;
    }

    private void BeginImpactSequence()
    {
        if (_hasImpacted) return;
        _hasImpacted = true;

        _isLaunched = false;
        if (_myCollider != null) _myCollider.enabled = false; 

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        StartCoroutine(WaitParticlesAndDestroy());
    }
    
    private IEnumerator WaitParticlesAndDestroy()
    {
        while (_particleSystem != null && _particleSystem.IsAlive(true))
        {
            if (_paused)
            {
                yield return null; 
            }
            yield return new WaitForSeconds(0.1f); 
        }
        
        Destroy(gameObject);
    }

    private IEnumerator LifetimeRoutine(float seconds)
    {
        yield return WaitForSecondsPausable(seconds, () => _paused);

        if (!_hasImpacted)
        {
            BeginImpactSequence();
        }
    }

    public void OnPauseChanged(bool paused) {
        _paused = paused;
        if (_particleSystem == null) return;
        if (_paused) _particleSystem.Pause(true);
        else if (!_hasImpacted) _particleSystem.Play(true);
    }
    
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, hitRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);

        if (Application.isPlaying && _isLaunched)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + _direction * 2f);
        }
    }
    
    #endregion
}