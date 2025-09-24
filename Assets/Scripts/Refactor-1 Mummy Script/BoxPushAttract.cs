using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Administra los triggers por cara del prefab y aplica desplazamientos suaves en eje X/Z.
/// - Cachea automáticamente los colliders hijos (Forward/Backward/Left/Right) y sus enums.
/// - Expone el eje de empuje y mueve la caja por transform/Rigidbody a la velocidad indicada.
/// - Antes de mover valida que haya suelo en la Layer "Floor" bajo la nueva posición.
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

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
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

        Vector3 axis = ResolveAxis(fromFace);
        if (axis.sqrMagnitude <= 0f)
        {
            return false;
        }

        Vector3 targetPosition = transform.position + axis * distance;
        if (!HasFloor(targetPosition))
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
            _rigidbody.MovePosition(targetPosition);
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

    private bool HasFloor(Vector3 targetPosition)
    {
        Vector3 origin = targetPosition + Vector3.up * _floorRayOriginHeight;
        float distance = _floorRayOriginHeight + _floorRayLength;
        return Physics.Raycast(origin, Vector3.down, distance, _floorMask, QueryTriggerInteraction.Ignore);
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
