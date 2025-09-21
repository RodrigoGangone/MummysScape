using UnityEngine;

/// <summary>
/// PushInfo
/// Paquete de datos inmutable sobre un empuje válido contra un BoxPushAttract.
/// Incluye la cara tocada, su normal proyectada al plano XZ, el eje permitido
/// (±X o ±Z), el centro de la cara y cuánto debe separarse el jugador.
/// </summary>
public readonly struct PushInfo
{
    public readonly BoxPushAttract Target;
    public readonly Collider FaceCollider;
    public readonly Vector3 FaceNormal;
    public readonly Vector3 MoveAxis;
    public readonly Vector3 FaceCenter;
    public readonly Vector3 ContactPoint;
    public readonly float PlayerDistance;

    public PushInfo(
        BoxPushAttract target,
        Collider faceCollider,
        Vector3 faceNormal,
        Vector3 moveAxis,
        Vector3 faceCenter,
        Vector3 contactPoint,
        float playerDistance)
    {
        Target = target;
        FaceCollider = faceCollider;
        FaceNormal = faceNormal;
        MoveAxis = moveAxis;
        FaceCenter = faceCenter;
        ContactPoint = contactPoint;
        PlayerDistance = playerDistance;
    }

    /// <summary>Posición deseada del pivot del jugador a la distancia indicada.</summary>
    public Vector3 PlayerAnchor => FaceCenter - FaceNormal * PlayerDistance;
}
