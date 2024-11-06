using System.Collections.Generic;
using UnityEngine;

public class CameraNode : MonoBehaviour
{
    // Enumeración para el eje de la curva Bézier
    public enum BezierAxis { X, Y, Z }

    public List<NodeConnection> connections;  // Lista de conexiones personalizadas

    public Vector3 Position { get; private set; }

    private CameraPathManager cameraPathManager;

    private void Start()
    {
        Position = transform.GetChild(0).position;

        cameraPathManager = Camera.main.GetComponent<CameraPathManager>();
        if (cameraPathManager == null)
        {
            Debug.LogError("CameraPathManager no encontrado en la cámara principal.");
        }
    }
    
    public NodeConnection GetConnectionToNode(CameraNode targetNode)
    {
        return connections.Find(connection => connection.targetNode == targetNode);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather") && cameraPathManager != null)
        {
            cameraPathManager.MoveCameraToNode(this);
        }
    }
}