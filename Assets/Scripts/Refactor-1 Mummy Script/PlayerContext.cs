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
    private readonly InteractionRuntime _interactions;
    private readonly GroundCheckRuntime _ground;

    public PlayerContext(
        Transform tf, Rigidbody rb,
        ICameraProvider camProvider,
        PlayerModel model, PlayerView view,
        MovementRuntime movement, IPlayerInput input, InteractionRuntime interactionRuntime,
        GroundCheckRuntime ground)
    {
        Tf = tf; Rb = rb; _camProvider = camProvider;
        Model = model; View = view; _movement = movement; Input = input;
        _interactions = interactionRuntime; _ground = ground;
    }

    private Camera Cam => _camProvider?.Current ?? Camera.main;
    public float MoveSpeed => _movement.MoveSpeed;
    public float TurnSpeed => _movement.TurnSpeed;
    public bool IsGrounded() => _ground != null && _ground.IsGrounded(Tf);

    /// <summary>Convierte input (x,y) a dirección de mundo relativa a cámara (plano XZ).</summary>
    public Vector3 CameraRelativeDir(float h, float v)
    {
        var cam = Cam;
        Vector3 fwd = cam ? cam.transform.forward : Vector3.forward;
        Vector3 right = cam ? cam.transform.right   : Vector3.right;
        fwd.y = 0f; right.y = 0f; fwd.Normalize(); right.Normalize();
        return (fwd * v + right * h).normalized;
    }
    
    /// <summary>Conveniencia: corre el PushChecker de InteractionRuntime.</summary>
    public bool TryGetPushTarget(out BoxPushAttract target, out RaycastHit left, out RaycastHit right)
    {
        target = null;
        left = default;
        right = default;
        return _interactions != null && _interactions.TryGetPushTarget(Tf, out target, out left, out right);
    }
}