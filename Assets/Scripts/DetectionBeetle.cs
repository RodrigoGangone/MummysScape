using System;
using System.Collections.Generic;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;

public class DetectionBeetle : MonoBehaviour
{
    public List<Collider> _beetles;
    public Rigidbody currentBeetle;

    private bool _isWall = false;

    private Transform _beetleFX;


    private void OnTriggerEnter(Collider other) // Agregar el Beetle con el que colisiono el trigger a la lista
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Beetle"))
        {
            _beetles.Add(other);
        }
    }

    private void OnTriggerStay(Collider other) //Recorrer una lista de collider para saber cual es el mas cercano
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Beetle") || _beetles.Count == 0) return;
        if (!isPosibleHook(other)) return;

        _beetleFX = other.transform.GetChild(0);

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

                    _beetleFX.gameObject.SetActive(true);
                }
                else
                {
                    _beetleFX.gameObject.SetActive(false);
                }
            }
        }
        else if (_beetles.Count == 1)
        {
            currentBeetle = _beetles[0].GetComponent<Rigidbody>();
            _beetleFX.gameObject.SetActive(true);
        }
    }


    private bool isPosibleHook(Collider other)
    {
        RaycastHit hit;
        Vector3 direction = other.transform.position - transform.position;
        float distance = direction.magnitude;
        int waterLayerMask = LayerMask.GetMask("Water");

        Debug.DrawLine(transform.position, other.transform.position, Color.red);

        if (Physics.Raycast(transform.position, direction, out hit, distance, waterLayerMask))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                Debug.Log("VERDADERO - Water detected");
                return false;
            }
        }
        
        return true;
    }

    private void OnTriggerExit(Collider other) // Eliminar el Beetle que agregue
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Beetle")) return;

        _beetleFX = other.transform.GetChild(0);
        _beetleFX.gameObject.SetActive(false);

        var cur = other.GetComponent<Rigidbody>();

        _beetles.Remove(other);

        if (currentBeetle == cur)
        {
            currentBeetle = null;
        }
    }
}