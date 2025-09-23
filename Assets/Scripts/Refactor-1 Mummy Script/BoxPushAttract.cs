using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Gestiona el movimiento cuadriculado de una caja empujable con detección por caras.
/// - Expone un enum con las caras del prefab (Forward, Backward, Left, Right).
/// - Mapea cada trigger hijo a su cara para recibir órdenes de movimiento desde InteractionRuntime.
/// - Mueve suavemente la caja (o su Rigidbody kinemático) si hay suelo en Layer "Floor" en la posición destino.
/// </summary>
[DisallowMultipleComponent]
public sealed class BoxPushAttract : MonoBehaviour
{
    public enum PushFace
    {
        Forward,
        Backward,
        Left,
        Right
    }

    private const float DistanceTolerance = 0.0001f;

    [Header("Movement")]
    [SerializeField] private float _stepDistance = 1f;
    [SerializeField] private float _moveSpeed = 3f;

    [Header("Floor Check")]
    [SerializeField] private float _floorRayOriginHeight = 0.5f;
    [SerializeField] private float _floorRayLength = 1.25f;
    [SerializeField] private LayerMask _floorMask = 0;

    private readonly Dictionary<Collider, PushFace> _facesByCollider = new();

    private Rigidbody _rigidbody;
    private Vector3 _targetPosition;
    private bool _isMoving;

    private void Awake()
    {
        CacheRigidBody();
        CacheFaces();
        InitializeTargetPosition();
        EnsureFloorMask();
    }

    private void OnValidate()
    {
        _stepDistance = Mathf.Max(0.01f, _stepDistance);
        _moveSpeed = Mathf.Max(0.01f, _moveSpeed);
        _floorRayOriginHeight = Mathf.Max(0f, _floorRayOriginHeight);
        _floorRayLength = Mathf.Max(0f, _floorRayLength);

        CacheRigidBody();
        EnsureFloorMask();

        if (!Application.isPlaying)
        {
            CacheFaces();
            InitializeTargetPosition();
        }
    }

    private void CacheRigidBody()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            return;
        }

        _rigidbody.isKinematic = true;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void CacheFaces()
    {
        _facesByCollider.Clear();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider == null || collider.transform == transform)
            {
                continue;
            }

            if (!collider.isTrigger)
            {
                collider.isTrigger = true;
            }

            if (TryParseFace(collider.transform.name, out PushFace face))
            {
                _facesByCollider[collider] = face;
            }
        }
    }

    private void InitializeTargetPosition()
    {
        _targetPosition = transform.position;
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

    private void Update()
    {
        if (!_isMoving)
        {
            return;
        }

        Vector3 current = transform.position;
        Vector3 next = Vector3.MoveTowards(current, _targetPosition, _moveSpeed * Time.deltaTime);

        if (_rigidbody != null)
        {
            _rigidbody.MovePosition(next);
        }
        else
        {
            transform.position = next;
        }

        if ((next - _targetPosition).sqrMagnitude <= DistanceTolerance)
        {
            if (_rigidbody == null)
            {
                transform.position = _targetPosition;
            }

            _isMoving = false;
        }
    }

    /// <summary>
    /// Intenta mover la caja una unidad de Step partiendo desde la cara que recibe el empuje.
    /// Retorna true si la orden fue aceptada y se inició el desplazamiento.
    /// </summary>
    /// <param name="fromFace">Cara desde la que empuja el jugador.</param>
    public bool TryMoveFrom(PushFace fromFace)
    {
        if (_isMoving)
        {
            return false;
        }

        Vector3 direction = GetWorldDirection(fromFace);
        if (direction.sqrMagnitude <= 0f)
        {
            return false;
        }

        Vector3 desiredPosition = transform.position + direction * _stepDistance;
        if (!HasFloor(desiredPosition))
        {
            return false;
        }

        _targetPosition = desiredPosition;
        _isMoving = true;
        return true;
    }

    /// <summary>True si actualmente la caja está desplazándose.</summary>
    public bool IsMoving => _isMoving;

    /// <summary>Intenta obtener la cara asociada al collider (trigger) recibido.</summary>
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

    private Vector3 GetWorldDirection(PushFace fromFace)
    {
        return fromFace switch
        {
            PushFace.Forward => -GetPlanarDirection(transform.forward),
            PushFace.Backward => GetPlanarDirection(transform.forward),
            PushFace.Left => GetPlanarDirection(transform.right),
            PushFace.Right => -GetPlanarDirection(transform.right),
            _ => Vector3.zero
        };
    }

    private static Vector3 GetPlanarDirection(Vector3 vector)
    {
        vector.y = 0f;
        return vector.sqrMagnitude > 0f ? vector.normalized : Vector3.zero;
    }

    private bool HasFloor(Vector3 desiredPosition)
    {
        Vector3 origin = desiredPosition + Vector3.up * _floorRayOriginHeight;
        float distance = _floorRayOriginHeight + _floorRayLength;
        return Physics.Raycast(origin, Vector3.down, distance, _floorMask, QueryTriggerInteraction.Ignore);
    }
}
