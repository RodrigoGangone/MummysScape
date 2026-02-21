using UnityEngine;
using UnityEngine.Pool;

/// <summary> 
/// Gestor de Disparo: Administra la creación y reutilización de proyectiles mediante un ObjectPool, 
/// respondiendo a los eventos de disparo del jugador para lanzar las vendas desde el punto de origen. 
/// </summary>

public class ShootingHandler : MonoBehaviour
{
    [Header("Pool Settings")] [SerializeField]
    private BandageProjectile _projectilePrefab;

    [SerializeField] private int _defaultCapacity = 5;
    [SerializeField] private int _maxCapacity = 10;

    [Header("Settings")] [SerializeField] private Transform _shootOrigin;
    [SerializeField] private float _shootSpeed = 30f;

    private IObjectPool<BandageProjectile> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<BandageProjectile>(CreateProjectile, OnTakeFromPool, OnReturnedToPool,
            OnDestroyPoolObject, true, _defaultCapacity, _maxCapacity);
    }

    private void OnShootTriggered()
    {
        if (SimpleShootData.Path == null) return;

        BandageProjectile projectile = _pool.Get();

        Vector3 startPos = (_shootOrigin != null) ? _shootOrigin.position : transform.position;

        projectile.Initialize(SimpleShootData.Path, _shootSpeed, startPos);
    }

    private BandageProjectile CreateProjectile()
    {
        var instance = Instantiate(_projectilePrefab);
        instance.SetPool(_pool);
        return instance;
    }

    private void OnTakeFromPool(BandageProjectile p) => p.gameObject.SetActive(true);
    private void OnReturnedToPool(BandageProjectile p) => p.gameObject.SetActive(false);
    private void OnDestroyPoolObject(BandageProjectile p) => Destroy(p.gameObject);
    
    private void OnEnable() => GameEventManager.Instance.playerEvents.OnShoot.Register(OnShootTriggered);
    private void OnDisable() => GameEventManager.Instance.playerEvents.OnShoot.Unregister(OnShootTriggered);
}