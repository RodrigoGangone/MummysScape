using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;
    [SerializeField] private GameObject _prefabBandage;
    private GameObject _target;
    [SerializeField] private GameObject _fxBullet;

    void OnEnable()
    {
        _target = GameObject.Find("Target");
        
        _rb.rotation = _target.transform.rotation;
        _rb.velocity = _target.transform.forward * _speed;
    }

    public void Reset()
    {
        transform.position = _target.transform.position;
    }

    public static void TurnOn(Bullet b)
    {
        b.Reset();
        b.gameObject.SetActive(true);
    }

    public static void TurnOff(Bullet b)
    {
        b.gameObject.SetActive(false);
    }

    private void OnCollisionEnter()
    {
        BulletFactory.Instance.ReturnObjectToPool(this);
        SpawnBandage();
    }

    private void SpawnBandage()
    {
        Instantiate(_prefabBandage, transform.position, Quaternion.identity);
        Instantiate(_fxBullet, transform.position, transform.rotation);
    }
}