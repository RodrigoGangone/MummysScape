using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPathManager : MonoBehaviour
{
    [SerializeField] private List<CameraNode> nodes;
    [SerializeField] private Transform target;

    private CameraNode currentNode;
    private float t = 0;
    private Vector3 startPoint;
    private Vector3 controlPoint;
    private Vector3 endPoint;
    private float currentSpeed;

    //CameraShake
    private bool isShaking = false;
    
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
        // Intenta obtener una conexión directa al nodo de destino
        var connection = currentNode?.GetConnectionToNode(targetNode);

        // Si no hay una conexión directa, usa valores predeterminados
        if (connection == null)
        {
            Debug.Log("No existe conexión directa. Usando valores predeterminados para la conexión.");
        
            connection = new NodeConnection
            {
                targetNode = targetNode,
                curveAxis = CameraNode.BezierAxis.Y,  // Eje Y por defecto
                curveIntensity = 10f,                 // Intensidad de 10 por defecto
                cameraSpeed = 1f                      // Velocidad de 1 por defecto
            };
        }

        // Establece los valores de conexión para el movimiento de la cámara
        currentNode = targetNode;
        startPoint = transform.position;
        endPoint = targetNode.Position;
    
        // Usa las propiedades específicas de la conexión (ya sea real o por defecto)
        controlPoint = CalculateControlPoint(startPoint, endPoint, connection.curveAxis, connection.curveIntensity);
        currentSpeed = connection.cameraSpeed;
    
        t = 0;
    }

    private void Update()
    {
        if (currentNode != null && Vector3.Distance(transform.position, currentNode.Position) > 0.1f)
        {
            // Calcular la distancia entre la cámara y el punto de destino
            float distance = Vector3.Distance(transform.position, endPoint);

            // Ajusta la velocidad según la distancia al objetivo. A menor distancia, menor velocidad.
            float dynamicSpeed = Mathf.Lerp(0.1f, currentSpeed, distance / 10f); 
            // El divisor (10f) se puede ajustar para cambiar la desaceleración. Cuanto mayor el divisor, menor la desaceleración.

            // Aumenta el valor de t basado en la velocidad ajustada
            t += dynamicSpeed * Time.deltaTime;
            t = Mathf.Clamp01(t);

            // Actualiza la posición de la cámara en la curva Bézier
            transform.position = CalculateBezierPoint(t, startPoint, controlPoint, endPoint);

            // Reinicia t al llegar al destino
            if (t >= 1f)
            {
                t = 0;
            }
        }
        
        if (target != null) transform.LookAt(target);

    }
    
    public void ShakeCamera(float duration, float magnitude)
    {
        if (!isShaking)
        {
            StartCoroutine(Shake(duration, magnitude));
        }
    }
    
    private IEnumerator Shake(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Posición base actual en cada frame del shake
            Vector3 currentBasePosition = transform.position;

            // Calcula un desplazamiento aleatorio
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.25f, 0.25f) * magnitude,
                Random.Range(-0.25f, 0.25f) * magnitude,
                Random.Range(-0.25f, 0.25f) * magnitude
            );

            // Aplica el desplazamiento aleatorio sobre la posición base
            transform.position = currentBasePosition + randomOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        isShaking = false; // Termina la sacudida
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