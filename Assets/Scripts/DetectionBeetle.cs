using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DetectionBeetle : MonoBehaviour
{
    public List<Collider> _beetles;
    public Collider currentBeetle;

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

                    currentBeetle = beetle;

                    currentBeetle.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
                }
                else
                {
                    beetle.transform.GetChild(0).GetComponent<ParticleSystem>().Stop();
                }
            }
        }
        else if (_beetles.Count == 1)
        {
            currentBeetle = _beetles[0];
            _beetles[0].transform.GetChild(0).GetComponent<ParticleSystem>().Play();
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
            other.transform.GetChild(0).GetComponent<ParticleSystem>().Stop();

            if (currentBeetle == other) currentBeetle = null;

            _beetles.Remove(other);
        }
    }
}