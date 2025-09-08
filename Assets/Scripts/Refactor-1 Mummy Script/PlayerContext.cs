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
    
    public bool TryGetPushTarget(out IPushable target, out PushInfo info)
    {
        target = null; info = default;
        return _interactions != null && _interactions.TryFindPushable(Tf, out target, out info);
    }
    
    /// <summary>Target al frente para Attract (si existe InteractionRuntime). </summary>
    public bool TryGetAttractTarget(out IAttractable target)
    {
        target = null;
        if (_interactions == null) return false;
        return _interactions.TryFindAttractable(Tf, out target, out _);
    }

    /// <summary>Target al frente para Swing (si existe InteractionRuntime). </summary>
    public bool TryGetSwingTarget(out ISwingable target)
    {
        target = null;
        if (_interactions == null) return false;
        return _interactions.TryFindSwingable(Tf, out target, out _);
    }
}