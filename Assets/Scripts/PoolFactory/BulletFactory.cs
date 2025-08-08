using UnityEngine;

public class BulletFactory : MonoBehaviour
{
    public static BulletFactory Instance { get; private set; } //Singleton

    [SerializeField] private Bullet _bulletPrefab;
    private Pool<Bullet> _myBulletPool;
    [SerializeField] private int _initialAmount;

    void Awake()
    {
        if (Instance) //Si ya hay un bulletFactory previo, este se destruye.
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _myBulletPool = new Pool<Bullet>(CreateObject, Bullet.TurnOn, Bullet.TurnOff, _initialAmount);
    }

    Bullet CreateObject()
    {
        return Instantiate(_bulletPrefab);
    }

    public Bullet GetObjectFromPool()
    {
        return _myBulletPool.GetObject();
    }

    public void ReturnObjectToPool(Bullet bullet)
    {
        _myBulletPool.ReturnObject(bullet);
    }
}