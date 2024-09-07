using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PushPullObject : MonoBehaviour
{
    private string playerTag = "PlayerFather"; // Tag para el jugador
    public float rayDistanceToPull = 7f; // Distancia de los raycasts
    public float rayDistanceToPush = 0.1f; // Distancia de los raycasts

    private BoxCollider _boxCollider;

    public LayerMask playerLayerMask; // Máscara de capa para el jugador
    [FormerlySerializedAs("groundLayer")] public LayerMask floorLayer;

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
    }

    public bool BoxInFloor()
    {
        Vector3 extents = _boxCollider.bounds.extents;

        Vector3 corner1 =
            transform.position + new Vector3(extents.x, -extents.y, extents.z); // Esquina delantera derecha
        Vector3 corner2 =
            transform.position + new Vector3(-extents.x, -extents.y, extents.z); // Esquina delantera izquierda
        Vector3 corner3 =
            transform.position + new Vector3(extents.x, -extents.y, -extents.z); // Esquina trasera derecha
        Vector3 corner4 =
            transform.position + new Vector3(-extents.x, -extents.y, -extents.z); // Esquina trasera izquierda


        if (Physics.Raycast(corner1, Vector3.down, rayDistanceToPush, floorLayer) &&
            Physics.Raycast(corner2, Vector3.down, rayDistanceToPush, floorLayer) &&
            Physics.Raycast(corner3, Vector3.down, rayDistanceToPush, floorLayer) &&
            Physics.Raycast(corner4, Vector3.down, rayDistanceToPush, floorLayer))
        {
            return true;
        }

        return false;
    }


    public String CheckPlayerRaycast()
    {
        // Direcciones desde las cuales lanzar los raycasts
        Vector3[] rayDirections =
        {
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
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistanceToPull, playerLayerMask))
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
        Gizmos.DrawRay(transform.position, transform.forward * rayDistanceToPull); // Forward
        Gizmos.DrawRay(transform.position, -transform.forward * rayDistanceToPull); // Backward
        Gizmos.DrawRay(transform.position, transform.right * rayDistanceToPull); // Right
        Gizmos.DrawRay(transform.position, -transform.right * rayDistanceToPull); // Left

        if (_boxCollider == null)
            _boxCollider = GetComponent<BoxCollider>();

        // Obtén las dimensiones del collider
        Vector3 extents = _boxCollider.bounds.extents;

        // Calcula las posiciones de las 4 esquinas
        Vector3 corner1 =
            transform.position + new Vector3(extents.x, -extents.y, extents.z); // Esquina delantera derecha
        Vector3 corner2 =
            transform.position + new Vector3(-extents.x, -extents.y, extents.z); // Esquina delantera izquierda
        Vector3 corner3 =
            transform.position + new Vector3(extents.x, -extents.y, -extents.z); // Esquina trasera derecha
        Vector3 corner4 =
            transform.position + new Vector3(-extents.x, -extents.y, -extents.z); // Esquina trasera izquierda

        // Dibuja los rayos como líneas en la escena
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(corner1, corner1 + Vector3.down * rayDistanceToPush); // Rayo desde la esquina 1
        Gizmos.DrawLine(corner2, corner2 + Vector3.down * rayDistanceToPush); // Rayo desde la esquina 2
        Gizmos.DrawLine(corner3, corner3 + Vector3.down * rayDistanceToPush); // Rayo desde la esquina 3
        Gizmos.DrawLine(corner4, corner4 + Vector3.down * rayDistanceToPush); // Rayo desde la esquina 4
    }
}