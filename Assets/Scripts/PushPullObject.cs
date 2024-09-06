using System;
using UnityEngine;

using UnityEngine;

public class PushPullObject : MonoBehaviour
{
    public float rayDistance = 7f; // Distancia de los raycasts
    public LayerMask playerLayerMask; // Máscara de capa para el jugador
    private string playerTag = "PlayerFather"; // Tag para el jugador

    private void Update()
    {
    }

    public String CheckPlayerRaycast()
    {
        // Direcciones desde las cuales lanzar los raycasts
        Vector3[] rayDirections = {
            transform.forward, 
            -transform.forward,
            transform.right,
            -transform.right
        };

        // Nombres de las direcciones correspondientes
        string[] directionNames = { "Forward", "Backward", "Right", "Left" };

        // Lanza un raycast desde cada dirección y verifica la colisión
        for (int i = 0; i < rayDirections.Length; i++)
        {
            Ray ray = new Ray(transform.position, rayDirections[i]);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, playerLayerMask))
            {
                // Verifica si el objeto colisionado tiene el tag correcto
                if (hit.collider.CompareTag(playerTag))
                {
                    return directionNames[i]; // Colisionó con el jugador
                }
            }
        }

        return null; // No colisionó con el jugador
    }

    // Método para visualizar los raycasts en la escena (opcional)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        // Visualización de raycasts en todas las direcciones
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance); // Forward
        Gizmos.DrawRay(transform.position, -transform.forward * rayDistance); // Backward
        Gizmos.DrawRay(transform.position, transform.right * rayDistance); // Right
        Gizmos.DrawRay(transform.position, -transform.right * rayDistance); // Left
    }
}
