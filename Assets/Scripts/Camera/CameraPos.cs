using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPos : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] List<Transform> positions;
    private int pos;
    float minDistance = 0.1f;
    private float speed = 0.05f;
    
    // Variables para el efecto de sacudida
    public float shakeDuration = 0.25f; // Duración del shake
    public float shakeMagnitude = 0.075f; // Intensidad del shake
    public float dampingSpeed = 1.0f; // Velocidad de atenuación
    private Vector3 initialPositionOffset; // Posición original con shake aplicado
    private float currentShakeDuration = 0f; // Tiempo restante de shake

    void OnEnable()
    {
        initialPositionOffset = Vector3.zero;
    }

    void FixedUpdate()
    {
        transform.LookAt(_player);
        
        if (Vector3.Distance(transform.position, positions[pos].position) > minDistance)
        {
            transform.position = Vector3.Lerp(transform.position, positions[pos].position, speed);
        }
        
        // Aplicar shake si está activo
        if (currentShakeDuration > 0)
        {
            initialPositionOffset = Random.insideUnitSphere * shakeMagnitude; // Genera un offset aleatorio
            currentShakeDuration -= Time.deltaTime * dampingSpeed; // Reduce la duración del shake
        }
        else
        {
            initialPositionOffset = Vector3.zero; // Restablece el offset
        }

        // Aplicar el offset de shake sobre la posición actual
        transform.position += initialPositionOffset;
    }

    public void SetCam(int pos)
    {
        this.pos = pos;
    }
    
    public void TriggerShake()
    {
        currentShakeDuration = shakeDuration; // Reinicia la duración del shake
    }
}