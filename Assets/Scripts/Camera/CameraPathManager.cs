using System.Collections.Generic;
using UnityEngine;

public class CameraPathManager : MonoBehaviour
{
    [SerializeField] private List<CameraNode> nodes;
    [SerializeField] private Transform player;

    private CameraNode currentNode;
    private float t = 0;
    private Vector3 startPoint;
    private Vector3 controlPoint;
    private Vector3 endPoint;
    private float currentSpeed;

    private void Start()
    {
        if (nodes.Count > 0)
        {
            currentNode = nodes[0];
            transform.position = currentNode.Position;
        }
    }

    public void MoveCameraToNode(CameraNode targetNode)
    {
        var connection = currentNode?.GetConnectionToNode(targetNode);

        if (connection != null)
        {
            currentNode = targetNode;
            startPoint = transform.position;
            endPoint = targetNode.Position;

            // Usa las propiedades específicas de la conexión
            controlPoint = CalculateControlPoint(startPoint, endPoint, connection.curveAxis, connection.curveIntensity);
            currentSpeed = connection.cameraSpeed;

            t = 0;
        }
        else
        {
            Debug.LogWarning("No existe conexión directa con el nodo de destino.");
        }
    }

    private void Update()
    {
        if (player != null)
        {
            transform.LookAt(player);
        }

        if (currentNode != null && Vector3.Distance(transform.position, currentNode.Position) > 0.1f)
        {
            t += currentSpeed * Time.deltaTime;
            t = Mathf.Clamp01(t);

            transform.position = CalculateBezierPoint(t, startPoint, controlPoint, endPoint);
        }
    }

    private Vector3 CalculateControlPoint(Vector3 start, Vector3 end, CameraNode.BezierAxis axis, float intensity)
    {
        Vector3 midpoint = (start + end) / 2;

        switch (axis)
        {
            case CameraNode.BezierAxis.X:
                return midpoint + Vector3.right * intensity;
            case CameraNode.BezierAxis.Y:
                return midpoint + Vector3.up * intensity;
            case CameraNode.BezierAxis.Z:
                return midpoint + Vector3.forward * intensity;
            default:
                return midpoint;
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
        if (nodes == null || nodes.Count < 2) return;

        Gizmos.color = Color.cyan;
        foreach (var node in nodes)
        {
            if (node == null) continue;

            foreach (var connection in node.connections)
            {
                if (connection.targetNode != null)
                {
                    Vector3 controlPt = CalculateControlPoint(
                        node.Position,
                        connection.targetNode.Position,
                        connection.curveAxis,
                        connection.curveIntensity
                    );
                    DrawBezierCurve(node.Position, controlPt, connection.targetNode.Position);
                }
            }
        }
    }

    private void DrawBezierCurve(Vector3 start, Vector3 control, Vector3 end)
    {
        int segmentCount = 20;
        Vector3 previousPoint = start;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 pointOnCurve = CalculateBezierPoint(t, start, control, end);
            Gizmos.DrawLine(previousPoint, pointOnCurve);
            previousPoint = pointOnCurve;
        }
    }
}