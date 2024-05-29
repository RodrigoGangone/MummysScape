using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;
    [SerializeField] private GameObject _prefabBandage;
    private GameObject _playerPos;
    private GameObject _target;

    void OnEnable()
    {
        _playerPos = GameObject.Find("Mummy");
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
        var bandageGO = Instantiate(_prefabBandage, transform.position, Quaternion.identity);
        PLAY_HIT_FX(bandageGO);
    }

    private void PLAY_HIT_FX(GameObject bandage)
    {
        var _hitBandage = bandage.transform.GetChild(0);

        _hitBandage.GetComponent<ParticleSystem>().Play();
    }
}