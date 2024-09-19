using System;
using UnityEngine;
using UnityEngine.Serialization;
using static Utils;

public class PushPullObject : MonoBehaviour
{
    private string playerTag = "PlayerFather"; // Tag para el jugador

    public float rayDistanceToPull = 7f; // Distancia de los raycasts
    public float raycastLengthToFloor = 1.75f; // Longitud del raycast
    public float raycastLengthToWall = 0.1f; // Longitud del raycast hacia las paredes

    private BoxCollider _boxCollider;

    public LayerMask playerLayerMask;
    private LayerMask floorLayerMask;
    private LayerMask wallLayerMask;

    [Header("GIZMOS")] 
    [SerializeField] public bool GizmoPull;
    [SerializeField] public bool GizmoWall;

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
        floorLayerMask = LayerMask.GetMask("Floor");
        wallLayerMask = LayerMask.GetMask("Wall");
    }

    public bool IsBoxCollisionWall(Vector3 dirToMove)
    {
        Vector3 extents = _boxCollider.bounds.extents;
        Vector3 center = _boxCollider.bounds.center;

        Vector3[] corners = GetFaceCorners(center, extents, dirToMove);
        
        foreach (var corner in corners)
        {
            if (Physics.Raycast(corner, dirToMove, raycastLengthToWall, wallLayerMask))
            {
                return true;
            }
        }

        return false;
    }

    private Vector3[] GetFaceCorners(Vector3 center, Vector3 extents, Vector3 direction)
    {
        var offset = 0.05f;
        
        if (direction == transform.forward) 
        {
            return new[]
            {
                center + transform.TransformDirection(new Vector3(extents.x, -extents.y + offset, extents.z)),
                center + transform.TransformDirection(new Vector3(-extents.x, -extents.y + offset, extents.z)),
                center + transform.TransformDirection(new Vector3(extents.x, extents.y - offset, extents.z)),
                center + transform.TransformDirection(new Vector3(-extents.x, extents.y - offset, extents.z))
            };
        }

        if (direction == -transform.forward) 
        {
            return new[]
            {
                center + transform.TransformDirection(new Vector3(extents.x, -extents.y + offset, -extents.z)),
                center + transform.TransformDirection(new Vector3(-extents.x, -extents.y + offset, -extents.z)),
                center + transform.TransformDirection(new Vector3(extents.x, extents.y - offset, -extents.z)),
                center + transform.TransformDirection(new Vector3(-extents.x, extents.y - offset, -extents.z)) 
            };
        }

        if (direction == transform.right)
        {
            return new[]
            {
                center + transform.TransformDirection(new Vector3(extents.x, -extents.y + offset, -extents.z)),
                center + transform.TransformDirection(new Vector3(extents.x, -extents.y + offset, extents.z)),
                center + transform.TransformDirection(new Vector3(extents.x, extents.y - offset, -extents.z)),
                center + transform.TransformDirection(new Vector3(extents.x, extents.y - offset, extents.z))
            };
        }

        if (direction == -transform.right)
        {
            return new[]
            {
                center + transform.TransformDirection(new Vector3(-extents.x, -extents.y + offset, -extents.z)), 
                center + transform.TransformDirection(new Vector3(-extents.x, -extents.y + offset, extents.z)),
                center + transform.TransformDirection(new Vector3(-extents.x, extents.y - offset, -extents.z)),
                center + transform.TransformDirection(new Vector3(-extents.x, extents.y - offset, extents.z))
            };
        }

        return new Vector3[0]; //dirección no valida
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
        var hitResult1 = Physics.Raycast(corner1, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);
        var hitResult2 = Physics.Raycast(corner2, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);
        var hitResult3 = Physics.Raycast(corner3, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);
        var hitResult4 = Physics.Raycast(corner4, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);

        // Debug para ver los raycasts en la escena
        Debug.DrawRay(corner1, Vector3.down * raycastLengthToFloor, hitResult1 ? Color.green : Color.red);
        Debug.DrawRay(corner2, Vector3.down * raycastLengthToFloor, hitResult2 ? Color.green : Color.red);
        Debug.DrawRay(corner3, Vector3.down * raycastLengthToFloor, hitResult3 ? Color.green : Color.red);
        Debug.DrawRay(corner4, Vector3.down * raycastLengthToFloor, hitResult4 ? Color.green : Color.red);

        // Retornar true solo si todos los raycasts hitean con algo
        return hitResult1 || hitResult2 || hitResult3 || hitResult4;
    }

    public string CheckPlayerRaycast()
    {
        Vector3[] rayDirections = { transform.forward, -transform.forward, transform.right, -transform.right };
        string[] directionNames = { BOX_SIDE_FORWARD, BOX_SIDE_BACKWARD, BOX_SIDE_RIGHT, BOX_SIDE_LEFT };
        float rayOffset = 0.5f;

        // Itera sobre cada dirección
        for (int i = 0; i < rayDirections.Length; i++)
        {
            // Calcula los offsets para los rayos (centro, izquierda, derecha)
            Vector3[] origins =
            {
                transform.position, // Centro
                transform.position - transform.right * rayOffset, // Izquierda
                transform.position + transform.right * rayOffset // Derecha
            };

            // Verifica si algún rayo en la dirección actual colisiona con el jugador
            foreach (var origin in origins)
            {
                if (Physics.Raycast(origin, rayDirections[i], out RaycastHit hit, rayDistanceToPull, playerLayerMask) &&
                    hit.collider.CompareTag(playerTag))
                {
                    return directionNames[i]; // Retorna la dirección si colisionó
                }
            }
        }

        return null; // No colisionó
    }

    private void OnDrawGizmos()
    {
        #region Check Pull

        if (GizmoPull)
        {
            Gizmos.color = Color.red;

            float rayOffset = 0.5f;

            // Dibuja los rayos para cada dirección: adelante, atrás, derecha e izquierda
            // Forward
            Gizmos.DrawRay(transform.position, transform.forward * rayDistanceToPull); // Forward Center
            Gizmos.DrawRay(transform.position - transform.right * rayOffset,
                transform.forward * rayDistanceToPull); // Forward Left
            Gizmos.DrawRay(transform.position + transform.right * rayOffset,
                transform.forward * rayDistanceToPull); // Forward Right

            // Backward
            Gizmos.DrawRay(transform.position, -transform.forward * rayDistanceToPull); // Backward Center
            Gizmos.DrawRay(transform.position - transform.right * rayOffset,
                -transform.forward * rayDistanceToPull); // Backward Left
            Gizmos.DrawRay(transform.position + transform.right * rayOffset,
                -transform.forward * rayDistanceToPull); // Backward Right

            // Right
            Gizmos.DrawRay(transform.position, transform.right * rayDistanceToPull); // Right Center
            Gizmos.DrawRay(transform.position - transform.forward * rayOffset,
                transform.right * rayDistanceToPull); // Right Left
            Gizmos.DrawRay(transform.position + transform.forward * rayOffset,
                transform.right * rayDistanceToPull); // Right Right

            // Left
            Gizmos.DrawRay(transform.position, -transform.right * rayDistanceToPull); // Left Center
            Gizmos.DrawRay(transform.position - transform.forward * rayOffset,
                -transform.right * rayDistanceToPull); // Left Left
            Gizmos.DrawRay(transform.position + transform.forward * rayOffset,
                -transform.right * rayDistanceToPull); // Left Right
        }

        #endregion

        #region CheckWall
        if (GizmoWall)
        {
            if (_boxCollider == null) return;

            Vector3 extents = _boxCollider.bounds.extents;
            Vector3 center = _boxCollider.bounds.center;
            Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };
            
            foreach (var direction in directions)
            {
                Vector3[] corners = GetFaceCorners(center, extents, direction);

                foreach (var corner in corners)
                {
                    bool hit = Physics.Raycast(corner, direction, raycastLengthToWall, wallLayerMask);
                    Gizmos.color = hit ? Color.red : Color.green;
                    Gizmos.DrawRay(corner, direction * raycastLengthToWall);
                }
            }
        }
        #endregion
    }
}