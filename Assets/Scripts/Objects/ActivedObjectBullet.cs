using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivedObjectBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platform;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            foreach (GameObject obj in _platform)
            {
                Destroy(obj);
            }
            
        }
    }
}