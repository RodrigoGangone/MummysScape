using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateObjectsBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platforms;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            foreach (GameObject platform in _platforms)
            {
                MovePlatform movePlatform = platform.GetComponent<MovePlatform>();
                if (movePlatform != null)
                    movePlatform.StartAction();
            }
        }
    }
}