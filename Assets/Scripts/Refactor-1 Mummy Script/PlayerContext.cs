using UnityEngine;

/// <summary>
/// PlayerContext
/// Reúne refs estables y proveedores dinámicos para que los States no dependan de públicos.
/// </summary>
public sealed class PlayerContext
{
    public readonly Transform Tf;
    public readonly Rigidbody Rb;
    public readonly PlayerModel Model;
    public readonly PlayerView View;
    public readonly IPlayerInput Input;

    private readonly ICameraProvider _camProvider;
    private readonly MovementRuntime _movement;

    public PlayerContext(
        Transform tf, Rigidbody rb,
        ICameraProvider camProvider,
        PlayerModel model, PlayerView view,
        MovementRuntime movement, IPlayerInput input)
    {
        Tf = tf; Rb = rb; _camProvider = camProvider;
        Model = model; View = view; _movement = movement; Input = input;
    }

    public Camera Cam => _camProvider?.Current ?? Camera.main;
    public float MoveSpeed => _movement.MoveSpeed;
    public float TurnSpeed => _movement.TurnSpeed;

    /// <summary>Convierte input (x,y) a dirección de mundo relativa a cámara (plano XZ).</summary>
    public Vector3 CameraRelativeDir(float h, float v)
    {
        var cam = Cam;
        Vector3 fwd = cam ? cam.transform.forward : Vector3.forward;
        Vector3 right = cam ? cam.transform.right   : Vector3.right;
        fwd.y = 0f; right.y = 0f; fwd.Normalize(); right.Normalize();
        return (fwd * v + right * h).normalized;
    }
}