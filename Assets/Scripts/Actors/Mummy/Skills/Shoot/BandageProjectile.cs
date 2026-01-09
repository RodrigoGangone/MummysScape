using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static PauseUtils;

[RequireComponent(typeof(Rigidbody))]
public class BandageProjectile : MonoBehaviour, IPausable
{
    [Header("Settings")] 
    [SerializeField] private LayerMask collisionLayers; 
    [SerializeField] private GameObject drop;
    [SerializeField] private TrailRenderer _trail; // (OPCIONAL) Arrástralo si tienes uno

    private Rigidbody _rb;
    private IObjectPool<BandageProjectile> _pool;
    private bool _paused;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        // Si no asignaste el trail en inspector, intenta buscarlo
        if (_trail == null) _trail = GetComponent<TrailRenderer>();
    }

    public void SetPool(IObjectPool<BandageProjectile> pool) => _pool = pool;

    // --- AHORA RECIBE startPos ---
    public void Initialize(IReadOnlyList<Vector3> path, float speed, Vector3 startPos)
    {
        // 1. LIMPIEZA VISUAL (Evita líneas raras al teleportarse)
        if (_trail != null) _trail.Clear();

        // 2. RESET FÍSICO DURO (Transform + Rigidbody)
        transform.position = startPos; 
        transform.rotation = Quaternion.identity;
        
        _rb.position = startPos; // <--- ESTO SOLUCIONA QUE SALGA DE LA ÚLTIMA POSICIÓN
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.useGravity = false;
        
        // 3. Iniciar lógica
        StopAllCoroutines(); // Seguridad extra por si quedó alguna corriendo
        StartCoroutine(RunPhysics(path, speed));
    }

    private IEnumerator RunPhysics(IReadOnlyList<Vector3> path, float speed)
    {
        // Pequeña espera de 1 frame si quieres asegurar que el Trail empiece limpio, 
        // pero generalmente _trail.Clear() basta.
        
        foreach (var target in path)
        {
            // Verificamos distancia
            while (Vector3.Distance(_rb.position, target) > 0.1f)
            {
                if (_paused)
                {
                    yield return WaitWhilePaused(() => _paused);
                    continue; 
                }

                // MovePosition interpola, es suave para físicas
                var newPosition = Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime);
                _rb.MovePosition(newPosition);

                // Rotación
                var dir = target - _rb.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    var targetRot = Quaternion.LookRotation(dir.normalized);
                    var newRot = Quaternion.Slerp(_rb.rotation, targetRot, Time.fixedDeltaTime * 15f);
                    _rb.MoveRotation(newRot);
                }

                yield return new WaitForFixedUpdate();
            }
        }

        _rb.useGravity = true;
        StartCoroutine(ReturnToPoolAfterTime(5f));
    }

    // ... (El resto de OnTriggerEnter, ReturnToPoolAfterTime, etc. sigue igual) ...
    
    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeSelf) return;

        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            if (drop != null) Instantiate(drop, transform.position, Quaternion.identity);
            ReleaseToPool();
        }
    }

    private IEnumerator ReturnToPoolAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        if (gameObject.activeSelf && _pool != null) _pool.Release(this);
        else if (gameObject.activeSelf) Destroy(gameObject);
    }
    
    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() 
    {
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
    }
}
public static class SimpleShootData
{
    public static List<Vector3> Path;
}