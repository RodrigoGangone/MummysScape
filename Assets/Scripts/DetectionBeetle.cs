using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DetectionBeetle : MonoBehaviour
{
    public List<Collider> _beetles;
    public Rigidbody currentBeetle;

    private void OnTriggerEnter(Collider other) // Agregar el Beetle con el que colisiono el trigger a la lista
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Beetle"))
        {
            _beetles.Add(other);
        }
    }

    private void OnTriggerStay(Collider other) //Recorrer una lista de collider para saber cual es el mas cercano
    {
        float nearestDistance = float.MaxValue;

        if (_beetles.Count > 1)
        {
            foreach (Collider beetle in _beetles)
            {
                float distance = Vector3.Distance(transform.position, beetle.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;

                    currentBeetle = beetle.GetComponent<Rigidbody>();

                    var obj = other.transform.GetChild(0);
                    obj.gameObject.SetActive(true);
                }
                else
                {
                    var obj = other.transform.GetChild(0);
                    obj.gameObject.SetActive(false);
                }
            }
        }
        else if (_beetles.Count == 1)
        {
            currentBeetle = _beetles[0].GetComponent<Rigidbody>();
            var obj = other.transform.GetChild(0);
            obj.gameObject.SetActive(true);
        }
        else
        {
            currentBeetle = null;
            return;
        }
    }

    private void OnTriggerExit(Collider other) // Eliminar el Beetle que agregue
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Beetle"))
        {
            var obj = other.transform.GetChild(0);
            obj.gameObject.SetActive(false);

            if (currentBeetle == other) currentBeetle = null;

            _beetles.Remove(other);
        }
    }
}