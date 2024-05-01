using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;

    void OnEnable()
    {
        _rb.AddForce(transform.forward*_speed, ForceMode.Impulse);
        
    }
    public void Reset()
    {
        var playerPos = GameObject.FindObjectOfType<BulletFactory>();
        transform.position = playerPos.transform.position;
        transform.rotation = playerPos.transform.rotation;
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
        }
    }
}
