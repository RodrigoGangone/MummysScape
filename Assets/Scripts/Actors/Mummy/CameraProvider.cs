using UnityEngine;

/// <summary> 
/// Servicio de Cámara: Provee una referencia centralizada y accesible a la cámara principal del juego, 
/// permitiendo el cambio dinámico de cámaras en tiempo real. 
/// </summary>

public interface ICameraProvider { Camera Current { get; } }

public sealed class CameraProvider : MonoBehaviour, ICameraProvider
{
    [SerializeField] private Camera _current;
    public Camera Current => _current != null ? _current : Camera.main;
    public void SetCurrent(Camera cam) => _current = cam;
}