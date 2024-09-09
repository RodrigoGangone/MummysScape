using System;
using UnityEngine;
using static Utils;

public class PushPullObject : MonoBehaviour
{
    private string playerTag = "PlayerFather"; // Tag para el jugador
    
    public float rayDistanceToPull = 7f; // Distancia de los raycasts
    public float raycastLength = 1.75f; // Longitud del raycast
    
    private BoxCollider _boxCollider;

    public LayerMask playerLayerMask;
    private LayerMask floorLayerMask;

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
        floorLayerMask = LayerMask.GetMask("Floor");
    }

    public bool BoxInFloor()
    {
        // Obtener las esquinas superiores de la caja
        Vector3 extents = _boxCollider.bounds.extents;
        Vector3 center = _boxCollider.bounds.center;

        // Esquinas superiores
        Vector3 corner1 = center + new Vector3(extents.x, extents.y, extents.z);
        Vector3 corner2 = center + new Vector3(-extents.x, extents.y, extents.z);
        Vector3 corner3 = center + new Vector3(extents.x, extents.y, -extents.z);
        Vector3 corner4 = center + new Vector3(-extents.x, extents.y, -extents.z);
        
        // Realizar raycasts desde las esquinas superiores hacia abajo
        var hitResult1 = Physics.Raycast(corner1, Vector3.down, out _, raycastLength, floorLayerMask);
        var hitResult2 = Physics.Raycast(corner2, Vector3.down, out _, raycastLength, floorLayerMask);
        var hitResult3 = Physics.Raycast(corner3, Vector3.down, out _, raycastLength, floorLayerMask);
        var hitResult4 = Physics.Raycast(corner4, Vector3.down, out _, raycastLength, floorLayerMask);
        
        // Debug para ver los raycasts en la escena
        Debug.DrawRay(corner1, Vector3.down * raycastLength, hitResult1 ? Color.green : Color.red);
        Debug.DrawRay(corner2, Vector3.down * raycastLength, hitResult2 ? Color.green : Color.red);
        Debug.DrawRay(corner3, Vector3.down * raycastLength, hitResult3 ? Color.green : Color.red);
        Debug.DrawRay(corner4, Vector3.down * raycastLength, hitResult4 ? Color.green : Color.red);

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
        string[] directionNames = { BOX_SIDE_FORWARD, BOX_SIDE_BACKWARD, BOX_SIDE_RIGHT, BOX_SIDE_LEFT };

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

        Gizmos.DrawRay(transform.position, transform.forward * rayDistanceToPull); // Forward
        Gizmos.DrawRay(transform.position, -transform.forward * rayDistanceToPull); // Backward
        Gizmos.DrawRay(transform.position, transform.right * rayDistanceToPull); // Right
        Gizmos.DrawRay(transform.position, -transform.right * rayDistanceToPull); // Left
        #endregion
    }
}