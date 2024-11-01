using System.Collections.Generic;
using UnityEngine;


public class CameraPathManager : MonoBehaviour
{
    [SerializeField] private List<CameraNode> nodes; // Lista de nodos con CameraNode
    [SerializeField] private Transform player;  // Referencia al jugador
    private CameraNode currentNode;
    private float speed = 1f;
    private float t = 0;
    private Vector3 startPoint;
    private Vector3 controlPoint;
    private Vector3 endPoint;
    private float initialSpeed = 1f; // Velocidad base
    private float minSpeed = 0.2f; // Velocidad mínima cerca del objetivo

    private void Start()
    {
        if (nodes.Count > 0)
        {
            currentNode = nodes[0]; // Nodo inicial
            transform.position = currentNode.Position;
        }
    }

    public void MoveCameraToNode(CameraNode targetNode)
    {
        if (currentNode != null && currentNode.connectedNodes.Contains(targetNode))
        {
            currentNode = targetNode;
            startPoint = transform.position;
            endPoint = targetNode.Position;
            controlPoint = (startPoint + endPoint) / 2 + Vector3.up * 5; // Punto de control para la curva Bézier
            t = 0;
        }
        else
        {
            Debug.LogWarning("No existe conexión directa con el nodo de destino.");
        }
    }

    private void Update()
    {
        // Asegurar que la cámara mire al jugador en todo momento
        if (player != null)
        {
            transform.LookAt(player);
        }

        // Movimiento suave de la cámara
        if (currentNode != null && Vector3.Distance(transform.position, currentNode.Position) > 0.1f)
        {
            // Ajusta la velocidad según la distancia al objetivo
            float distance = Vector3.Distance(transform.position, currentNode.Position);
            float dynamicSpeed = Mathf.Lerp(minSpeed, initialSpeed, distance / 5f); // Ajusta el divisor para cambiar la desaceleración
            t += dynamicSpeed * Time.deltaTime;
            t = Mathf.Clamp01(t);

            // Movimiento en la curva Bézier
            transform.position = CalculateBezierPoint(t, startPoint, controlPoint, endPoint);
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }

    private void OnDrawGizmos()
    {
        if (nodes == null) return;

        Gizmos.color = Color.cyan;
        foreach (var node in nodes)
        {
            if (node == null) continue;

            foreach (var connectedNode in node.connectedNodes)
            {
                if (connectedNode != null)
                {
                    DrawBezierCurve(node.Position, connectedNode.Position);
                }
            }
        }
    }

// Método para dibujar una curva Bézier entre dos puntos
    private void DrawBezierCurve(Vector3 start, Vector3 end)
    {
        Vector3 controlPoint = (start + end) / 2 + Vector3.up * 5; // Ajusta la altura del punto de control para modificar la curva
        int segmentCount = 20; // Número de segmentos para la curva, cuanto mayor sea, más suave será la curva

        Vector3 previousPoint = start;
        for (int i = 1; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 pointOnCurve = CalculateBezierPoint(t, start, controlPoint, end);
            Gizmos.DrawLine(previousPoint, pointOnCurve);
            previousPoint = pointOnCurve;
        }
    }
}