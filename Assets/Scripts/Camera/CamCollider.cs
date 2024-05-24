using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamCollider : MonoBehaviour
{
    [SerializeField] public int posCam;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Camera.main.GetComponent<CameraPos>().SetCam(posCam);
        }
    }
}
