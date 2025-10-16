using UnityEngine;

/// <summary>
/// CameraProvider
/// Proveedor de la cámara actual. Si no hay asignada, usa Camera.main.
/// Permite SetCurrent(cam) en runtime si cambiás de cámara física.
/// </summary>
public interface ICameraProvider { Camera Current { get; } }

public sealed class CameraProvider : MonoBehaviour, ICameraProvider
{
    [SerializeField] private Camera _current;
    public Camera Current => _current != null ? _current : Camera.main;
    public void SetCurrent(Camera cam) => _current = cam;
}