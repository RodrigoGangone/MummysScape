using System;
using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Implementa IPushable para una caja (RB + BoxCollider) alineada a los ejes locales del prefab.
/// - Decide el eje permitido según la CARA donde empuja el player: cara ±X => eje local Z; cara ±Z => eje local X.
/// - Mueve la caja sólo sobre ese eje usando Rigidbody.MovePosition y bloquea con Physics.BoxCast (skin configurable).
/// - Entrega SnapPoint = centro horizontal de la cara, para centrar al player (soft-snap) mientras dura el push.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class BoxPushAttract : MonoBehaviour
{
    private const float FaceDetectionEpsilon = 0.9f;

    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;
    private FaceCache[] _faces = Array.Empty<FaceCache>();

    private void Awake()
    {
        CacheDependencies();
        RebuildFaces();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheDependencies();
        if (_boxCollider != null)
        {
            RebuildFaces();
        }
    }
#endif

    /// <summary>
    /// Devuelve el eje de movimiento ya invertido (hacia el interior de la cara) para la normal consultada.
    /// </summary>
    /// <param name="normalWorld">Normal de la cara en espacio mundial (normalizada).</param>
    public Vector3 GetMoveAxisFromWorldNormal(Vector3 normalWorld)
    {
        FaceCache face = FindFaceByNormal(normalWorld);
        return face.MoveAxisWorld;
    }

    /// <summary>
    /// Recupera el SnapPoint en espacio mundial asociado a la cara cuya normal coincide con el parámetro.
    /// </summary>
    /// <param name="normalWorld">Normal de la cara en espacio mundial (normalizada).</param>
    public Vector3 GetSnapPointFromWorldNormal(Vector3 normalWorld)
    {
        FaceCache face = FindFaceByNormal(normalWorld);
        return face.SnapPointWorld;
    }

    private void CacheDependencies()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider>();
        }
    }

    private void RebuildFaces()
    {
        if (_boxCollider == null)
        {
            _faces = Array.Empty<FaceCache>();
            return;
        }

        _faces = new[]
        {
            CreateFace(Vector3.right),
            CreateFace(Vector3.left),
            CreateFace(Vector3.forward),
            CreateFace(Vector3.back),
        };
    }

    private FaceCache CreateFace(Vector3 normalLocal)
    {
        Vector3 normalWorld = transform.TransformDirection(normalLocal).normalized;
        Vector3 moveAxisWorld = -normalWorld;

        Vector3 halfSize = _boxCollider.size * 0.5f;
        Vector3 faceCenterLocal = _boxCollider.center + new Vector3(
            normalLocal.x * halfSize.x,
            0f,
            normalLocal.z * halfSize.z);
        faceCenterLocal.y = _boxCollider.center.y;

        Vector3 snapPointWorld = transform.TransformPoint(faceCenterLocal);

        return new FaceCache(normalLocal, normalWorld, moveAxisWorld, snapPointWorld);
    }

    private FaceCache FindFaceByNormal(Vector3 normalWorld)
    {
        if (_faces.Length == 0)
        {
            throw new InvalidOperationException("No hay caras cacheadas. Asegurate de llamar a RebuildFaces antes de consultar.");
        }

        normalWorld = normalWorld.normalized;

        FaceCache bestFace = _faces[0];
        float bestDot = Vector3.Dot(bestFace.NormalWorld, normalWorld);

        for (int i = 1; i < _faces.Length; i++)
        {
            float dot = Vector3.Dot(_faces[i].NormalWorld, normalWorld);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = _faces[i];
            }
        }

        if (bestDot < FaceDetectionEpsilon)
        {
            throw new ArgumentException($"La normal {normalWorld} no coincide con ninguna cara registrada.", nameof(normalWorld));
        }

        return bestFace;
    }

    private readonly struct FaceCache
    {
        public Vector3 NormalLocal { get; }
        public Vector3 NormalWorld { get; }
        public Vector3 MoveAxisWorld { get; }
        public Vector3 SnapPointWorld { get; }

        public FaceCache(Vector3 normalLocal, Vector3 normalWorld, Vector3 moveAxisWorld, Vector3 snapPointWorld)
        {
            NormalLocal = normalLocal;
            NormalWorld = normalWorld;
            MoveAxisWorld = moveAxisWorld;
            SnapPointWorld = snapPointWorld;
        }
    }
}
