using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;
    [SerializeField] GameObject _fxBullet;
    [SerializeField] Player _player;
    private GameObject _target;
    void OnEnable()
    {
        _target = GameObject.Find("Target");
        _player = FindObjectOfType<Player>();
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
        SpawnMummyBandage();
    }

    private void SpawnMummyBandage()
    {
        BulletFactory.Instance.ReturnObjectToPool(this);
        //_player._modelPlayer.SpawnBandage(transform);
        _player._modelPlayer.CreateBandageAtPosition(transform.position);
        Instantiate(_fxBullet, transform.position, transform.rotation);
    }
}