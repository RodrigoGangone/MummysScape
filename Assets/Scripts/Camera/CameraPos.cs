using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPos : MonoBehaviour
{
    [SerializeField] private Transform _player; // Referencia al jugador para hacer que la cámara mire al jugador
    [SerializeField] List<Transform> positions; // Lista de posiciones a las que la cámara puede moverse
    public int pos; // Índice de la posición objetivo
    float minDistance = 0.01f; // Distancia mínima para considerar que se llegó al objetivo
    [SerializeField] private float speed = 0.5f; // Velocidad del movimiento
    private float t = 0; // Variable para controlar la progresión a lo largo de la curva Bézier
    private Vector3 startPoint; // Punto de inicio del movimiento
    private Vector3 controlPoint; // Punto de control para la curva de Bézier
    private Vector3 endPoint; // Punto final (objetivo) del movimiento

    void Start()
    {
        // Inicializa la cámara en la posición inicial
        startPoint = transform.position;
        endPoint = positions[pos].position;
        controlPoint = (startPoint + endPoint) / 2 + Vector3.up * 5; // Punto de control básico para la curva
    }

    void FixedUpdate()
    {
        // Mover la cámara utilizando una curva de Bézier solo si no ha llegado a la posición objetivo
        if (Vector3.Distance(transform.position, positions[pos].position) > minDistance)
        {
            t += speed * Time.deltaTime; // Incrementa el tiempo basado en la velocidad
            t = Mathf.Clamp01(t); // Asegura que t se mantenga entre 0 y 1

            // Interpolación Bézier cuadrática
            transform.position = CalculateBezierPoint(t, startPoint, controlPoint, endPoint);

            // Mira hacia el jugador
            transform.LookAt(_player);
        }
    }
    
    void LateUpdate()
    {
        // Asegurar que la cámara siempre mire al jugador
        transform.LookAt(_player);
    }

    // Método para configurar la nueva posición de la cámara
    public void SetCam(int pos)
    {
        this.pos = pos;
        startPoint = transform.position; // Reiniciar el punto de inicio a la posición actual
        endPoint = positions[pos].position; // Nuevo objetivo
        controlPoint = (startPoint + endPoint) / 2 + Vector3.up * 5; // Calcular un nuevo punto de control
        t = 0; // Reiniciar t para la nueva interpolación
    }

    // Método para calcular un punto en una curva de Bézier cuadrática
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2); // Fórmula de la curva Bézier cuadrática
    }
}