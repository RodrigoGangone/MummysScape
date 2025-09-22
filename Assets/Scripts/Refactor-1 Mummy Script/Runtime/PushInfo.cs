using UnityEngine;

/// <summary>
/// PushInfo
/// Contiene datos inmutables del empuje activo: normal de la cara, punto de contacto y referencias al objetivo.
/// Expone utilidades para recuperar el centro horizontal del cuerpo del objeto empujable.
/// </summary>
public readonly struct PushInfo
{
    public PushInfo(BoxPushAttract pushable, Vector3 faceNormal, Vector3 contactPoint)
    {
        Pushable = pushable;
        FaceNormal = faceNormal;
        ContactPoint = contactPoint;
    }

    public BoxPushAttract Pushable { get; }
    public Vector3 FaceNormal { get; }
    public Vector3 ContactPoint { get; }

    /// <summary>
    /// Centro horizontal del cuerpo del objeto empujable en coordenadas de mundo.
    /// </summary>
    public Vector3 HorizontalBodyCenter => Pushable != null ? Pushable.HorizontalBodyCenter : Vector3.zero;

    /// <summary>
    /// Vector horizontal normalizado desde un origen dado hasta el centro del cuerpo. Retorna Vector3.zero si no hay magnitud.
    /// </summary>
    public Vector3 GetHorizontalDirectionFrom(Vector3 origin)
    {
        Vector3 target = HorizontalBodyCenter;
        Vector3 from = new(origin.x, target.y, origin.z);
        Vector3 direction = target - from;
        direction.y = 0f;
        float sqrMag = direction.sqrMagnitude;
        if (sqrMag <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        return direction / Mathf.Sqrt(sqrMag);
    }
}
