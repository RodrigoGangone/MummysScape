using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;
    [SerializeField] private GameObject _platform;
    private GameObject _playerPos;
    private GameObject _target;

    void OnEnable()
    {
        //_playerPos = FindObjectOfType<BulletFactory>();

        _playerPos = GameObject.Find("Mummy");
        _target = GameObject.Find("Target");
        
        _rb.AddForce(_playerPos.transform.forward * _speed, ForceMode.Impulse);
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            BulletFactory.Instance.ReturnObjectToPool(this);

            var originalSize = new Vector3(1, 1, 1);

            if (_playerPos.transform.localScale.x < originalSize.x ||
                _playerPos.transform.localScale.y < originalSize.y ||
                _playerPos.transform.localScale.z < originalSize.z)
            {
                _playerPos.transform.localScale += new Vector3(0.1f, 0.1f, 0.1f);
            }
        }
    }
}