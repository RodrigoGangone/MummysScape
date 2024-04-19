using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BandageBullet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground")) { Destroy(gameObject); }
    }
}
