using UnityEngine;

[System.Serializable]
public class NodeConnection
{
    public CameraNode targetNode;  // Nodo de destino
    public CameraNode.BezierAxis curveAxis = CameraNode.BezierAxis.Y;  // Eje de la curva
    public float curveIntensity = 10f;  // Intensidad de la curva
    public float cameraSpeed = 1f;  // Velocidad de la cámara para esta conexión
}