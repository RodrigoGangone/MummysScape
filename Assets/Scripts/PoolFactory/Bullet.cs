using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;
    [SerializeField] private GameObject _platform;
    [SerializeField] private GameObject _prefabBandage;
    private GameObject _playerPos;
    private GameObject _target;

    void OnEnable()
    {
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
        //Instancio un prefab de la bandaje cuando choca la bullet con la wall, y le paso referecia del bullet
        if (!collision.gameObject.CompareTag("Wall")) return;
        
        var bandageGO = Instantiate(_prefabBandage, transform.position, Quaternion.identity);
        Bandage bandageScript = bandageGO.GetComponent<Bandage>();
        bandageScript.setInstantiator(this);
        gameObject.SetActive(false);
    }
}