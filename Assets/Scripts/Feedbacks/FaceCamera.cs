using UnityEngine;

/// <summary> 
/// Comportamiento Billboard: Asegura que el objeto esté siempre orientado hacia la cámara principal, 
/// siendo esencial para indicadores de interfaz o iconos que deben ser legibles en el mundo 3D. 
/// </summary>

public class FaceCamera : MonoBehaviour
{
    void Update() => transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
}
