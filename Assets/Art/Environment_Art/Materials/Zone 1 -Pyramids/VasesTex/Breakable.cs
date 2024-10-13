using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private GameObject _destroyeVersion;
    [SerializeField] private GameObject _drop;
    [SerializeField] private bool _droped;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet") || other.gameObject.CompareTag("PlayerFather"))
        {
            Instantiate(_destroyeVersion, transform.position, transform.rotation);

            if (_droped)
                Instantiate(_drop, transform.position, transform.rotation);

            Destroy(gameObject);
        }
    }
}