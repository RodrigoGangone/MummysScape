using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Administra los triggers por cara del prefab y aplica desplazamientos suaves en eje X/Z.
/// - Cachea automáticamente los colliders hijos (Forward/Backward/Left/Right) y sus enums.
/// - Expone el eje de empuje y mueve la caja por transform/Rigidbody a la velocidad indicada.
/// - Antes de mover valida que haya suelo en la Layer "Floor" bajo la nueva posición y dibuja
///   gizmos de depuración tanto del chequeo actual como del candidato.
/// - Conmuta automáticamente entre un cuerpo cinemático (para empuje suave) y dinámico con
///   gravedad cuando la caja queda suspendida.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class BoxPushAttract : MonoBehaviour
{
    public enum PushFace
    {
        Forward,
        Backward,
        Left,
        Right
    }

    [Header("Floor Check")]
    [SerializeField] private float _floorRayOriginHeight = 0.5f;
    [SerializeField] private float _floorRayLength = 1.25f;
    [SerializeField] private LayerMask _floorMask = 0;

    private readonly Dictionary<Collider, PushFace> _facesByCollider = new();

    private Rigidbody _rigidbody;
    private RigidbodyConstraints _kinematicConstraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    private RigidbodyConstraints _dynamicConstraints = RigidbodyConstraints.FreezeRotation;

    private Vector3 _currentCheckOrigin;
    private Vector3 _currentCheckEnd;
    private bool _currentCheckHit;

    private Vector3 _targetCheckOrigin;
    private Vector3 _targetCheckEnd;
    private bool _targetCheckHit;

    private void Awake()
    {
        CacheRigidBody();
        CacheFaces();
        EnsureFloorMask();
    }

    private void OnValidate()
    {
        _floorRayOriginHeight = Mathf.Max(0f, _floorRayOriginHeight);
        _floorRayLength = Mathf.Max(0f, _floorRayLength);
        CacheRigidBody();
        EnsureFloorMask();

        if (!Application.isPlaying)
        {
            CacheFaces();
        }
    }

    private void CacheRigidBody()
    {
        if (!TryGetComponent(out _rigidbody))
        {
            return;
        }

        _kinematicConstraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _dynamicConstraints = RigidbodyConstraints.FreezeRotation;

        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        SyncBodyPhysicsState(!Application.isPlaying || _rigidbody.isKinematic);
    }

    private void CacheFaces()
    {
        _facesByCollider.Clear();

        Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (Collider collider in colliders)
        {
            if (collider == null || collider.transform == transform)
            {
                continue;
            }

            collider.isTrigger = true;

            if (TryParseFace(collider.transform.name, out PushFace face))
            {
                _facesByCollider[collider] = face;
            }
        }
    }

    private void EnsureFloorMask()
    {
        if (_floorMask.value != 0)
        {
            return;
        }

        int floorLayer = LayerMask.NameToLayer("Floor");
        if (floorLayer >= 0)
        {
            _floorMask = 1 << floorLayer;
        }
    }

    /// <summary>Intenta obtener la cara asociada al collider trigger recibido.</summary>
    public bool TryGetFace(Collider collider, out PushFace face)
    {
        if (collider != null && _facesByCollider.TryGetValue(collider, out face))
        {
            return true;
        }

        if (collider != null && TryParseFace(collider.transform.name, out face))
        {
            _facesByCollider[collider] = face;
            return true;
        }

        face = default;
        return false;
    }

    /// <summary>Retorna el eje mundial (X o Z) hacia donde se moverá la caja al empujar desde la cara indicada.</summary>
    public Vector3 GetPushAxis(PushFace fromFace)
    {
        return ResolveAxis(fromFace);
    }

    /// <summary>
    /// Intenta mover la caja una distancia (speed * deltaTime) respetando el eje permitido.
    /// Retorna true si la traslación se concretó.
    /// </summary>
    public bool TryMove(PushFace fromFace, float distance)
    {
        if (!(distance > 0f))
        {
            return false;
        }

        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            Vector3 origin = _rigidbody.position + Vector3.up * _floorRayOriginHeight;
            Vector3 end = origin + Vector3.down * (_floorRayOriginHeight + _floorRayLength);
            RecordFloorCheck(origin, FloorCheckType.Target, false, end);
            return false;
        }

        Vector3 axis = ResolveAxis(fromFace);
        if (axis.sqrMagnitude <= 0f)
        {
            return false;
        }

        Vector3 originPosition = _rigidbody != null ? _rigidbody.position : transform.position;
        Vector3 targetPosition = originPosition + axis * distance;
        targetPosition.y = originPosition.y;

        if (!HasFloor(targetPosition, FloorCheckType.Target))
        {
            return false;
        }

        MoveTo(targetPosition);
        return true;
    }

    private void MoveTo(Vector3 targetPosition)
    {
        if (_rigidbody != null)
        {
            if (_rigidbody.isKinematic)
            {
                _rigidbody.MovePosition(targetPosition);
            }
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private Vector3 ResolveAxis(PushFace fromFace)
    {
        Vector3 localDirection = fromFace switch
        {
            PushFace.Forward => Vector3.back,
            PushFace.Backward => Vector3.forward,
            PushFace.Left => Vector3.right,
            PushFace.Right => Vector3.left,
            _ => Vector3.zero
        };

        Vector3 world = transform.TransformDirection(localDirection);
        world.y = 0f;
        if (world.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        world.Normalize();
        return Mathf.Abs(world.x) >= Mathf.Abs(world.z)
            ? new Vector3(Mathf.Sign(world.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(world.z));
    }

    private bool HasFloor(Vector3 targetPosition, FloorCheckType gizmoType)
    {
        Vector3 origin = targetPosition + Vector3.up * _floorRayOriginHeight;
        float distance = _floorRayOriginHeight + _floorRayLength;
        bool hit = Physics.Raycast(origin, Vector3.down, out RaycastHit hitInfo, distance, GetFloorMask(), QueryTriggerInteraction.Ignore);

        Vector3 end = hit ? hitInfo.point : origin + Vector3.down * distance;
        RecordFloorCheck(origin, gizmoType, hit, end);

        return hit;
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null)
        {
            return;
        }

        bool hasFloor = HasFloor(_rigidbody.position, FloorCheckType.Current);
        if (hasFloor)
        {
            if (!_rigidbody.isKinematic)
            {
                SyncBodyPhysicsState(true);
            }
        }
        else if (_rigidbody.isKinematic)
        {
            SyncBodyPhysicsState(false);
        }
    }

    private void SyncBodyPhysicsState(bool kinematic)
    {
        if (_rigidbody == null)
        {
            return;
        }

        _rigidbody.isKinematic = kinematic;
        _rigidbody.useGravity = !kinematic;
        _rigidbody.constraints = kinematic ? _kinematicConstraints : _dynamicConstraints;

        if (kinematic)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RecordFloorCheck(Vector3 origin, FloorCheckType type, bool hit, Vector3 end)
    {
        switch (type)
        {
            case FloorCheckType.Current:
                _currentCheckOrigin = origin;
                _currentCheckEnd = end;
                _currentCheckHit = hit;
                break;
            case FloorCheckType.Target:
                _targetCheckOrigin = origin;
                _targetCheckEnd = end;
                _targetCheckHit = hit;
                break;
        }
    }

    private enum FloorCheckType
    {
        Current,
        Target
    }

    private void OnDrawGizmos()
    {
        DrawFloorCheckGizmo(_currentCheckOrigin, _currentCheckEnd, _currentCheckHit, new Color(0.2f, 0.6f, 1f));
        DrawFloorCheckGizmo(_targetCheckOrigin, _targetCheckEnd, _targetCheckHit, Color.yellow);

        if (_currentCheckOrigin == Vector3.zero && _targetCheckOrigin == Vector3.zero)
        {
            Vector3 origin = transform.position + Vector3.up * _floorRayOriginHeight;
            float distance = _floorRayOriginHeight + _floorRayLength;
            Vector3 end = origin + Vector3.down * distance;
            bool hit = Physics.Raycast(origin, Vector3.down, out RaycastHit hitInfo, distance, GetFloorMask(), QueryTriggerInteraction.Ignore);
            end = hit ? hitInfo.point : end;
            DrawFloorCheckGizmo(origin, end, hit, new Color(0.2f, 0.6f, 1f));
        }
    }

    private static void DrawFloorCheckGizmo(Vector3 origin, Vector3 end, bool hit, Color color)
    {
        if (origin == Vector3.zero && end == Vector3.zero)
        {
            return;
        }

        Color previous = Gizmos.color;
        Gizmos.color = hit ? color : Color.red;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawSphere(end, 0.05f);
        Gizmos.color = previous;
    }

    private int GetFloorMask()
    {
        return _floorMask.value != 0 ? _floorMask.value : Physics.DefaultRaycastLayers;
    }

    private static bool TryParseFace(string name, out PushFace face)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            face = default;
            return false;
        }

        switch (name.Trim().ToLowerInvariant())
        {
            case "forward":
                face = PushFace.Forward;
                return true;
            case "backward":
                face = PushFace.Backward;
                return true;
            case "left":
                face = PushFace.Left;
                return true;
            case "right":
                face = PushFace.Right;
                return true;
            default:
                face = default;
                return false;
        }
    }
}
