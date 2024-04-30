using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody _rb;
    [SerializeField] float _speed;
    // Start is called before the first frame update
    void Start()
    {
        _rb?.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void OnEnable()
    {
        _rb.AddForce(Vector3.forward, ForceMode.Impulse);
    }
}
