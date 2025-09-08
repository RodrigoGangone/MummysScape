using UnityEngine;

/// <summary>
/// Contratos de objetos interactuables exclusivos (uno a la vez).
/// - IExclusiveInteractable: lock por usuario.
/// - IPushable / IAttractable / ISwingable: capacidades específicas.
/// Mantiene el acoplamiento bajo: los States sólo hablan contra estas interfaces.
/// </summary>
public interface IExclusiveInteractable
{
    bool IsBusy { get; }
    bool TryAcquire(Object user);
    void Release(Object user);
}

public readonly struct PushInfo
{
    public readonly Vector3 FaceNormal;   // Normal de la cara de contacto (mundo)
    public readonly Vector3 Axis;         // Eje permitido (mundo) => +X / -X / +Z / -Z
    public readonly Vector3 SnapPoint;    // Punto a donde “pegar” al player
    public PushInfo(Vector3 n, Vector3 axis, Vector3 snap) { FaceNormal = n; Axis = axis; SnapPoint = snap; }
}

public interface IPushable : IExclusiveInteractable
{
    // Determina si el player está en una cara válida para empujar.
    bool TryGetPushInfo(Transform player, float maxFaceAngleDeg, float maxDist, out PushInfo info);
    // Vida del push
    void OnPushStart(in PushInfo info);
    void OnPushUpdate(in PushInfo info, float signedInput01, float speed);
    void OnPushEnd();
}

public interface IAttractable : IExclusiveInteractable
{
    bool CanAttract(Transform player, float maxDist, out Vector3 allowedAxis); // eje permitido X o Z
    void OnAttractStart();
    void OnAttractUpdate(Vector3 playerPos, Vector3 allowedAxis, float speed);
    void OnAttractEnd();
}

public interface ISwingable : IExclusiveInteractable
{
    bool CanSwing(Transform player, float maxDist, out Vector3 attachPoint);
    // El State decide si usar SpringJoint; el interactuable no crea componentes en el player.
}