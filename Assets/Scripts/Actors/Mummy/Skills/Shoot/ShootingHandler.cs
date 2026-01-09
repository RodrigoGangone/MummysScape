using UnityEngine;
using UnityEngine.Pool;

public class ShootingHandler : MonoBehaviour
{
    // ... (Tus settings iguales) ...
    [Header("Pool Settings")]
    [SerializeField] private BandageProjectile _projectilePrefab;
    [SerializeField] private int _defaultCapacity = 5;
    [SerializeField] private int _maxCapacity = 10;

    [Header("Settings")] 
    [SerializeField] private Transform _shootOrigin;
    [SerializeField] private float _shootSpeed = 30f;

    private IObjectPool<BandageProjectile> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<BandageProjectile>(CreateProjectile, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, _defaultCapacity, _maxCapacity);
    }

    private void OnEnable() => GameEventManager.Instance.playerEvents.OnShoot.Register(OnShootTriggered);
    private void OnDisable()
    {
        if(GameEventManager.Instance != null)
            GameEventManager.Instance.playerEvents.OnShoot.Unregister(OnShootTriggered);
    } 

    private void OnShootTriggered()
    {
        if (SimpleShootData.Path == null) return;
        
        BandageProjectile projectile = _pool.Get();

        // --- CAMBIO: Determinamos posición aquí y la pasamos ---
        Vector3 startPos = (_shootOrigin != null) ? _shootOrigin.position : transform.position;

        // Pasamos la posición al proyectil para que él haga el reset físico atómico
        projectile.Initialize(SimpleShootData.Path, _shootSpeed, startPos);
    }

    // --- POOL METHODS ---
    private BandageProjectile CreateProjectile()
    {
        var instance = Instantiate(_projectilePrefab);
        instance.SetPool(_pool);
        return instance;
    }
    private void OnTakeFromPool(BandageProjectile p) => p.gameObject.SetActive(true);
    private void OnReturnedToPool(BandageProjectile p) => p.gameObject.SetActive(false);
    private void OnDestroyPoolObject(BandageProjectile p) => Destroy(p.gameObject);
}