using System.Collections;
using UnityEngine;
using static Utils;

public class PushPullObject : MonoBehaviour
{
    [Header("BANDAGE AROUND")] [SerializeField]
    private GameObject[] _bandagesAroundBox;

    private Coroutine currentCoroutine; // Para almacenar la coroutine activa
    private float wrapSpeed = 0.5f; // Velocidad de envoltura/desenvoltura
    private float currentOffset = 0f; // Offset actual del material

    [Header("GIZMOS")] [SerializeField] public bool GizmoPull;
    [SerializeField] public bool GizmoWall;


    private BoxCollider _boxCollider;

    public float rayDistanceToPull = 7f; // Distancia de los raycasts
    public float raycastLengthToFloor = 1.75f; // Longitud del raycast
    public float raycastLengthToWall = 0.1f; // Longitud del raycast hacia las paredes

    public LayerMask playerLayerMask;
    private LayerMask floorLayerMask;
    private LayerMask wallLayerMask;

    private void Start()
    {
        _boxCollider = GetComponent<BoxCollider>();
        floorLayerMask = LayerMask.GetMask("Floor", "Box");
        wallLayerMask = LayerMask.GetMask("Wall");

        SetBandageOffset(0f);
    }

    #region Handler Bandage Material

    public void StartWrap() //iniciar proceso de envolverse
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateBandages(0f, 1f));
    }

    public void StartUnwrap() //iniciar proceso de desenvolverse
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(AnimateBandages(currentOffset, 0f));
    }

    public void SetExplode(bool explode)
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
        if (PerformBoxCast(direction))
        {
            //si no esta tocando el suelo Explode = true  y desenvolver
            SetExplode(true);
            StartUnwrap();
            return true;
        }

        return false;
    }

    private bool PerformBoxCast(Vector3 direction)
    {
        Vector3 center = _boxCollider.bounds.center;
        Vector3 localOffset = Vector3.zero;
        Vector3 halfExtents = Vector3.one;

        if (direction == Vector3.forward || direction == Vector3.back)
        {
            halfExtents = new Vector3(_boxCollider.bounds.extents.x, _boxCollider.bounds.extents.y, 0.05f); // Fino en Z
            localOffset = (direction == Vector3.forward ? transform.forward : -transform.forward) *
                          _boxCollider.bounds.extents.z;
        }
        else if (direction == Vector3.right || direction == Vector3.left)
        {
            halfExtents = new Vector3(0.05f, _boxCollider.bounds.extents.y, _boxCollider.bounds.extents.z); // Fino en X
            localOffset = (direction == Vector3.right ? transform.right : -transform.right) *
                          _boxCollider.bounds.extents.x;
        }

        Vector3 origin = center + localOffset;

        int layerMask = LayerMask.GetMask("Wall", "MovableBox");

        float boxCastDistance = 0.05f;
        RaycastHit hit;
        bool isHit = Physics.BoxCast(origin, halfExtents, direction, out hit, transform.rotation, boxCastDistance,
            layerMask);

        return isHit; // Retorna si colisiona con un objeto
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
            SetExplode(!inFloor);
            StartUnwrap();
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
        Vector3 halfExtents = Vector3.one;
        if (direction == Vector3.forward || direction == Vector3.back)
        {
            halfExtents = new Vector3(_boxCollider.bounds.extents.x, _boxCollider.bounds.extents.y, 0.05f); // Fino en Z
            localOffset = (direction == Vector3.forward ? transform.forward : -transform.forward) *
                          _boxCollider.bounds.extents.z;
        }
        else if (direction == Vector3.right || direction == Vector3.left)
        {
            halfExtents = new Vector3(0.05f, _boxCollider.bounds.extents.y, _boxCollider.bounds.extents.z); // Fino en X
            localOffset = (direction == Vector3.right ? transform.right : -transform.right) *
                          _boxCollider.bounds.extents.x;
        }

        Vector3 origin = center + localOffset;

        int layerMask = LayerMask.GetMask("Wall", "MovableBox");

        RaycastHit hit;
        float boxCastDistance = 1f;
        bool isHit = Physics.BoxCast(origin,
            halfExtents, direction,
            out hit, transform.rotation,
            0.05f, layerMask);

        if (isHit)
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.green;

        Gizmos.matrix = Matrix4x4.TRS(origin, transform.rotation, Vector3.one);
        // Dibujo la caja un poco mas pequeña para evitar errores
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 1.9f);
    }
}