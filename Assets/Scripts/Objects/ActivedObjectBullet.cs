using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivedObjectBullet : MonoBehaviour
{
    [SerializeField] private GameObject _platform;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            Destroy(_platform);
        }
    }
}