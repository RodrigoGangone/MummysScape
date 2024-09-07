using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PushPullObject : MonoBehaviour
{
    private string playerTag = "PlayerFather"; // Tag para el jugador
    
    public float rayDistanceToPull = 7f; // Distancia de los raycasts
    public float raycastLength = 1f; // Longitud del raycast
    public float raycastOffsetY = 0.5f; // Offset adicional hacia arriba y abajo en Y


    private BoxCollider _boxCollider;

    public LayerMask playerLayerMask; // Máscara de capa para el jugador
    private LayerMask floorLayerMask;

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
        floorLayerMask = LayerMask.GetMask("Floor");
    }

    private void Update()
    {
        BoxInFloor();
    }

    public bool BoxInFloor()
    {
        Vector3 extents = _boxCollider.bounds.extents;
        Vector3 center = _boxCollider.bounds.center;

        // Esquinas de la parte inferior de la caja, movidas 0.5 unidades hacia arriba
        Vector3 corner1 = center + new Vector3(extents.x, -extents.y + raycastOffsetY, extents.z);
        Vector3 corner2 = center + new Vector3(-extents.x, -extents.y + raycastOffsetY, extents.z);
        Vector3 corner3 = center + new Vector3(extents.x, -extents.y + raycastOffsetY, -extents.z);
        Vector3 corner4 = center + new Vector3(-extents.x, -extents.y + raycastOffsetY, -extents.z);

        RaycastHit hit1, hit2, hit3, hit4;
        
        // Raycasts hacia abajo desde cada esquina, con longitud de 1 unidad
        bool hitResult1 = Physics.Raycast(corner1, Vector3.down, out hit1, raycastLength, floorLayerMask);
        bool hitResult2 = Physics.Raycast(corner2, Vector3.down, out hit2, raycastLength, floorLayerMask);
        bool hitResult3 = Physics.Raycast(corner3, Vector3.down, out hit3, raycastLength, floorLayerMask);
        bool hitResult4 = Physics.Raycast(corner4, Vector3.down, out hit4, raycastLength, floorLayerMask);

        // Imprimir el nombre del objeto con el que hitea cada raycast, si hay colisión
        if (hitResult1)
        {
            Debug.Log($"Corner 1 hit: {hit1.collider.gameObject.name}");
        }

        if (hitResult2)
        {
            Debug.Log($"Corner 2 hit: {hit2.collider.gameObject.name}");
        }

        if (hitResult3)
        {
            Debug.Log($"Corner 3 hit: {hit3.collider.gameObject.name}");
        }

        if (hitResult4)
        {
            Debug.Log($"Corner 4 hit: {hit4.collider.gameObject.name}");
        }

        // Retornar true solo si todos los raycasts hitean con algo
        return hitResult1 || hitResult2 || hitResult3 || hitResult4;
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
        #region Check Pull
        Gizmos.color = Color.red;

        // Visualización de raycasts en todas las direcciones
        Gizmos.DrawRay(transform.position, transform.forward * rayDistanceToPull); // Forward
        Gizmos.DrawRay(transform.position, -transform.forward * rayDistanceToPull); // Backward
        Gizmos.DrawRay(transform.position, transform.right * rayDistanceToPull); // Right
        Gizmos.DrawRay(transform.position, -transform.right * rayDistanceToPull); // Left
        #endregion

        #region Check Floor
        if (_boxCollider == null)
            _boxCollider = GetComponent<BoxCollider>();

        Vector3 extents = _boxCollider.bounds.extents;
        Vector3 center = _boxCollider.bounds.center;

        // Esquinas de la parte inferior de la caja, movidas 0.5 unidades hacia arriba
        Vector3 corner1 = center + new Vector3(extents.x, -extents.y + raycastOffsetY, extents.z);
        Vector3 corner2 = center + new Vector3(-extents.x, -extents.y + raycastOffsetY, extents.z);
        Vector3 corner3 = center + new Vector3(extents.x, -extents.y + raycastOffsetY, -extents.z);
        Vector3 corner4 = center + new Vector3(-extents.x, -extents.y + raycastOffsetY, -extents.z);

        Gizmos.color = BoxInFloor() ? Color.blue : Color.red;
        Gizmos.DrawLine(corner1, corner1 + Vector3.down * raycastLength);
        Gizmos.DrawLine(corner2, corner2 + Vector3.down * raycastLength);
        Gizmos.DrawLine(corner3, corner3 + Vector3.down * raycastLength);
        Gizmos.DrawLine(corner4, corner4 + Vector3.down * raycastLength);
        #endregion
    }
}