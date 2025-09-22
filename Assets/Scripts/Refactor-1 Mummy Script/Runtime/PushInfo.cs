using UnityEngine;

/// <summary>
/// PushInfo
/// Representa la cara de un volumen empujable contra la que se alinea el jugador.
/// Expone el Anchor para posicionar al personaje a partir del centro de la cara y
/// la normal opuesta con la distancia deseada.
/// </summary>
public readonly struct PushInfo
{
    public Vector3 FaceCenter { get; }
    public Vector3 FaceNormal { get; }
    public float PlayerDistance { get; }

    public Vector3 Anchor => FaceCenter - FaceNormal * PlayerDistance;

    public PushInfo(Vector3 faceCenter, Vector3 faceNormal, float playerDistance)
    {
        FaceCenter = faceCenter;
        FaceNormal = faceNormal.sqrMagnitude > 0f ? faceNormal.normalized : Vector3.forward;
        PlayerDistance = Mathf.Max(0f, playerDistance);
    }
}
