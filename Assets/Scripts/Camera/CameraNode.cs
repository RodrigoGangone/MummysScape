using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraNode : MonoBehaviour
{
    [Tooltip("Nodos a los que este nodo puede conectarse.")]
    public List<CameraNode> connectedNodes;  // Lista de nodos con los que puede conectar
    public Vector3 Position { get; private set; }
    
    private CameraPathManager cameraPathManager;

    private void Start()
    {
        //La posicion del primer hije
        Position = transform.GetChild(0).position;
        
        cameraPathManager = Camera.main.GetComponent<CameraPathManager>();
        if (cameraPathManager == null)
        {
            Debug.LogError("CameraPathManager no encontrado en la cámara principal.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather") && cameraPathManager != null && this != null)
        {
            cameraPathManager.MoveCameraToNode(this);
        }
    }
}