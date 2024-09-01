using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabObject : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
        {
            Player player = other.GetComponent<Player>();
            player.GrabbedObject = transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
        {
            Player player = other.GetComponent<Player>();
            player.GrabbedObject = null;
        }
    }
}