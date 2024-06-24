using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private GameObject _destroyeVersion;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet") || other.gameObject.CompareTag("PlayerFather"))
        { 
            Instantiate(_destroyeVersion, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }
}
