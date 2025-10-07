using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;
    [SerializeField] GameObject _fxBullet;
    [SerializeField] Player _player;
    [SerializeField] private float _lifeTime = 5f;

    private GameObject _target;

    void OnEnable()
    {
        _target = GameObject.Find("Target");
        _player = FindObjectOfType<Player>();

        _rb.rotation = _target.transform.rotation;
        _rb.velocity = _target.transform.forward * _speed;

        StartCoroutine(LifeTime());
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

    IEnumerator LifeTime()
    {
        // Espera el tiempo definido (_lifeTime) y luego apaga el objeto
        yield return new WaitForSeconds(_lifeTime);
        BulletFactory.Instance.ReturnObjectToPool(this);
    }

    private void OnCollisionEnter()
    {
        SpawnMummyBandage();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Button"))
        {
            AudioManager.Instance.PlaySFX(NameSounds.SFX_BandageBlow);
            SpawnMummyBandage();
        }
    }

    private void SpawnMummyBandage()
    {
        BulletFactory.Instance.ReturnObjectToPool(this);
        _player._modelPlayer.CreateBandageAtPosition(transform.position);
        Instantiate(_fxBullet, transform.position, transform.rotation);
    }
}