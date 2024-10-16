using System;
using System.Collections;
using UnityEngine;
using static Utils;

public class PushPullObject : MonoBehaviour
{
    private Coroutine currentCoroutine; // Para almacenar la coroutine activa
    private float wrapSpeed = 0.5f; // Velocidad de envoltura/desenvoltura
    private float currentOffset = 0f; // Offset actual del material

    private BoxCollider _boxCollider;

    public LayerMask playerLayerMask;
    private LayerMask floorLayerMask;
    private LayerMask wallLayerMask;

    [Header("FXs")] [SerializeField] private ParticleSystem sandMoundParticle;

    [Header("BANDAGE AROUND")] [SerializeField]
    private GameObject[] _bandagesAroundBox;

    [Header("GIZMOS")] [SerializeField] public bool GizmoPull;
    [SerializeField] public bool GizmoWall;

    [SerializeField] private float rayDistanceToPull = 7f; // Distancia de los raycasts
    [SerializeField] private float raycastLengthToFloor = 1.75f; // Longitud del raycast
    [SerializeField] private float raycastLengthToWall = 0.1f; // Longitud del raycast hacia las paredes

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
        floorLayerMask = LayerMask.GetMask("Floor", "Box");
        wallLayerMask = LayerMask.GetMask("Wall");

        SetBandageOffset(0f);
    }

    #region Handler Bandage Material

    public void StartWrap() //Iniciar proceso de envolverse
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateBandages(0f, 1f));
    }

    public void StartUnwrap() //Iniciar proceso de desenvolverse
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateBandages(currentOffset, 0f));
    }
    
    public void StartExplode() //Iniciar proceso de Explosion
    {
        SetExplode(true);
        StartUnwrap();
    }

    private void SetExplode(bool explode) //Recorro lista de vendas y le activo el explode
    {
        foreach (GameObject bandage in _bandagesAroundBox)
        {
            Material mat = bandage.GetComponent<Renderer>().material;
            mat.SetFloat("_Explode", explode ? 1f : 0f);
        }
    }

    private IEnumerator AnimateBandages(float startOffset, float endOffset)
    {
        float elapsedTime = 0f;
        currentOffset = startOffset;

        while (Mathf.Abs(currentOffset - endOffset) > 0.01f)
        {
            currentOffset = Mathf.Lerp(startOffset, endOffset, elapsedTime / wrapSpeed);
            SetBandageOffset(currentOffset);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SetBandageOffset(endOffset);
    }

    private void SetBandageOffset(float offset)
    {
        foreach (GameObject bandage in _bandagesAroundBox)
        {
            Material mat = bandage.GetComponent<Renderer>().material;
            mat.SetFloat("_Offset", offset);
        }
    }

    #endregion

    #region Check Collisions With Wall/Floor

    public bool CheckCollisionInDirections(Vector3 direction)
    {
        if (PerformBoxCast(direction)) return true;
        return false;
    }

    private bool PerformBoxCast(Vector3 direction)
    {
        Vector3 center = _boxCollider.bounds.center;
        Vector3 localOffset = Vector3.zero;

        // Obtenemos el tamaño completo del BoxCollider usando bounds.size y no los extents
        Vector3 boxSize = new Vector3(
            _boxCollider.bounds.size.x,
            _boxCollider.bounds.size.y * 0.9f,  // Ajustamos el tamaño de Y como en Gizmos
            _boxCollider.bounds.size.z);

        var offsetZ = new Vector3(0, 0, 0.02f);
        var offsetX = new Vector3(0.02f, 0, 0);

        if (direction == Vector3.forward || direction == Vector3.back)
        {
            boxSize.z = 0.025f; // Pequeño valor en Z para evitar colisiones innecesarias
            localOffset = direction == Vector3.forward ?
                transform.forward * _boxCollider.bounds.extents.x + offsetZ :
                -transform.forward * _boxCollider.bounds.extents.x - offsetZ;
        }
        else if (direction == Vector3.right || direction == Vector3.left)
        {
            boxSize.x = 0.025f; // Pequeño valor en X para evitar colisiones innecesarias
            localOffset = direction == Vector3.right ?
                transform.right * _boxCollider.bounds.extents.x + offsetX
                : -transform.right * _boxCollider.bounds.extents.x - offsetX;
        }

        Vector3 origin = center + localOffset;

        // Definimos las capas con las que el BoxCast interactuará
        int layerMask = LayerMask.GetMask("Wall", "Box", "Collectible");

        // Usamos OverlapBox para detectar objetos en el área definida por boxSize
        Collider[] hits = Physics.OverlapBox(origin, boxSize * 0.5f, transform.rotation, layerMask);

        // Si hay colisiones, retornamos verdadero
        bool isHit = hits.Length > 0; 

        return isHit;
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
        Vector3 centerCheck = center + new Vector3(0, extents.y, 0);

        // Realizar raycasts desde las esquinas superiores hacia abajo
        var hitResult1 = Physics.Raycast(corner1, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);
        var hitResult2 = Physics.Raycast(corner2, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);
        var hitResult3 = Physics.Raycast(corner3, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);
        var hitResult4 = Physics.Raycast(corner4, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);
        var hitResult5 = Physics.Raycast(centerCheck, Vector3.down, out _, raycastLengthToFloor, floorLayerMask);

        // Debug para ver los raycasts en la escena
        Debug.DrawRay(corner1, Vector3.down * raycastLengthToFloor, hitResult1 ? Color.green : Color.red);
        Debug.DrawRay(corner2, Vector3.down * raycastLengthToFloor, hitResult2 ? Color.green : Color.red);
        Debug.DrawRay(corner3, Vector3.down * raycastLengthToFloor, hitResult3 ? Color.green : Color.red);
        Debug.DrawRay(corner4, Vector3.down * raycastLengthToFloor, hitResult4 ? Color.green : Color.red);
        Debug.DrawRay(centerCheck, Vector3.down * raycastLengthToFloor, hitResult5 ? Color.green : Color.red);

        // Retornar true solo si todos los raycasts hitean con algo
        var inFloor = hitResult1 || hitResult2 || hitResult3 || hitResult4 || hitResult5;

        //si no esta tocando el suelo Explode = true  y desenvolver
        if (!inFloor)
        {
            StartExplode();
        }

        return inFloor;
    }

    #endregion

    public string CheckPlayerRaycast()
    {
        Vector3[] rayDirections = { transform.forward, -transform.forward, transform.right, -transform.right };
        string[] directionNames = { BOX_SIDE_FORWARD, BOX_SIDE_BACKWARD, BOX_SIDE_RIGHT, BOX_SIDE_LEFT };
        float rayOffset = 0.75f;

        // Centros y offsets para las diferentes direcciones (adelante, atrás, derecha, izquierda)
        Vector3[] forwardOrigins =
        {
            transform.position, // Centro
            transform.position - transform.right * rayOffset, // Izquierda
            transform.position + transform.right * rayOffset // Derecha
        };

        Vector3[] backwardOrigins =
        {
            transform.position, // Centro
            transform.position - transform.right * rayOffset, // Izquierda
            transform.position + transform.right * rayOffset // Derecha
        };

        Vector3[] rightOrigins =
        {
            transform.position, // Centro
            transform.position - transform.forward * rayOffset, // Izquierda
            transform.position + transform.forward * rayOffset // Derecha
        };

        Vector3[] leftOrigins =
        {
            transform.position, // Centro
            transform.position - transform.forward * rayOffset, // Izquierda
            transform.position + transform.forward * rayOffset // Derecha
        };

        // Almacena las posiciones de origen por cada dirección
        Vector3[][] originsArray = { forwardOrigins, backwardOrigins, rightOrigins, leftOrigins };

        // Itera sobre cada dirección y origen
        for (int i = 0; i < rayDirections.Length; i++)
        {
            // Para cada dirección, itera sobre las posiciones de origen (centro, izquierda, derecha)
            foreach (var origin in originsArray[i])
            {
                if (Physics.Raycast(origin, rayDirections[i], out RaycastHit hit, rayDistanceToPull, playerLayerMask) &&
                    hit.collider.CompareTag(PLAYER_TAG))
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

            float rayOffset = 0.75f;

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

        #region Gizmo Wall Detection

        if (GizmoWall)
        {
            // Dibuja los boxcasts en las direcciones de movimiento
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            foreach (var direction in directions)
            {
                PerformBoxCastGizmos(direction);
            }
        }

        #endregion
    }

    private void PerformBoxCastGizmos(Vector3 direction)
    {
        Vector3 center = _boxCollider.bounds.center;
        Vector3 localOffset = Vector3.zero;

        // Obtenemos el tamaño completo del BoxCollider usando bounds.size y no los extents
        Vector3 boxSize = new Vector3(
            _boxCollider.bounds.size.x,
            _boxCollider.bounds.size.y * 0.9f,
            _boxCollider.bounds.size.z);

        var offsetZ = new Vector3(0, 0, 0.02f);
        var offsetX = new Vector3(0.02f, 0, 0);

        if (direction == Vector3.forward || direction == Vector3.back)
        {
            boxSize.z = 0.025f; // Pequeño valor en Z para evitar colisiones innecesarias
            localOffset = direction == Vector3.forward ?
                transform.forward * _boxCollider.bounds.extents.x + offsetZ :
                -transform.forward * _boxCollider.bounds.extents.x - offsetZ;
        }
        else if (direction == Vector3.right || direction == Vector3.left)
        {
            boxSize.x = 0.025f; // Pequeño valor en X para evitar colisiones innecesarias
            localOffset = direction == Vector3.right ?
                transform.right * _boxCollider.bounds.extents.x + offsetX
                : -transform.right * _boxCollider.bounds.extents.x - offsetX;
        }

        Vector3 origin = center + localOffset;

        int layerMask = LayerMask.GetMask("Wall", "Box", "Collectible");

        // Usamos OverlapBox para detectar objetos en el área definida por boxSize
        Collider[] hits = Physics.OverlapBox(origin, boxSize * 0.5f, transform.rotation, layerMask);

        bool isHit = hits.Length > 0; // Si hay colisiones, isHit será verdadero

        // Cambiamos el color del Gizmo dependiendo de si hubo colisión o no
        Gizmos.color = isHit ? Color.red : Color.green;

        // Dibujamos el Gizmo usando el tamaño correcto de la caja
        Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.name == "FloorBox")
        {
            if (!sandMoundParticle.isPlaying)
                sandMoundParticle.Play();
        }
    }
}