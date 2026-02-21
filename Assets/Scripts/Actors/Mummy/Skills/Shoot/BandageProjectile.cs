using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static PauseUtils;

/// <summary> 
/// Lógica de Proyectil de Venda: Controla el desplazamiento físico de la venda a través de una 
/// ruta de puntos, gestionando su propia física, detección de colisiones y ciclo de vida 
/// mediante un pool de objetos. 
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public class BandageProjectile : MonoBehaviour, IPausable
{
    [Header("Settings")] 
    [SerializeField] private LayerMask collisionLayers; 
    [SerializeField] private GameObject drop;

    private Rigidbody _rb;
    private Collider _collider;
    private IObjectPool<BandageProjectile> _pool;
    private bool _paused;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        
        _collider = GetComponent<Collider>();
    }

    public void SetPool(IObjectPool<BandageProjectile> pool) => _pool = pool;

    public void Initialize(IReadOnlyList<Vector3> path, float speed, Vector3 startPos)
    {
        transform.position = startPos; 
        transform.rotation = Quaternion.identity;
        
        _rb.position = startPos;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.useGravity = false;
        
        if (_collider != null) _collider.enabled = false;
        
        StopAllCoroutines();
        StartCoroutine(RunPhysics(path, speed));
    }

    private IEnumerator RunPhysics(IReadOnlyList<Vector3> path, float speed)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 target = path[i];

            if (_collider != null && !_collider.enabled)
            {
                if (i >= path.Count - 5)
                {
                    _collider.enabled = true;
                }
            }

            while (Vector3.Distance(_rb.position, target) > 0.1f)
            {
                if (_paused)
                {
                    yield return WaitWhilePaused(() => _paused);
                    continue; 
                }

                var newPosition = Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime);
                _rb.MovePosition(newPosition);

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
        if (_collider != null) _collider.enabled = true;
        StartCoroutine(ReturnToPoolAfterTime(5f));
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeSelf) return;

        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            if (drop != null)
            {
                var bandage = Instantiate(drop, transform.position, Quaternion.identity);
                
                bandage.GetComponent<Bandage>().SetupPickupDelay();
            }
            
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
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}

public static class SimpleShootData
{
    public static List<Vector3> Path;
}