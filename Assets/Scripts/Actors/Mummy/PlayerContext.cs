using UnityEngine;

/// <summary> 
/// Contenedor de Dependencias: Provee a los estados de la FSM un acceso seguro y simplificado a 
/// las referencias del mundo, sistemas de detección y componentes del jugador sin generar acoplamiento directo. 
/// </summary>

public sealed class PlayerContext
{
    public readonly Transform Tf;
    public readonly Rigidbody Rb;
    public readonly PlayerModel Model;
    public readonly PlayerView View;
    public readonly IPlayerInput Input;
    public readonly SwingHandler SwingHandler;
    public readonly EnvironmentObserver Observer;
    public readonly StateMachinePlayer StateMachine;

    private readonly ICameraProvider _camProvider;
    private readonly MovementRuntime _movement;
    private readonly InteractionRuntime _interactions;
    private readonly GroundCheckRuntime _ground;

    public PlayerContext(
        Transform tf, Rigidbody rb,
        SwingHandler swingHandler, EnvironmentObserver observer,
        ICameraProvider camProvider,
        PlayerModel model, PlayerView view,
        MovementRuntime movement, IPlayerInput input, InteractionRuntime interactionRuntime,
        GroundCheckRuntime ground, StateMachinePlayer sm)
    {
        Tf = tf;
        Rb = rb;
        SwingHandler = swingHandler;
        Observer = observer;
        _camProvider = camProvider;
        Model = model;
        View = view;
        _movement = movement;
        Input = input;
        _interactions = interactionRuntime;
        _ground = ground;
        StateMachine = sm;
    }

    private Camera Cam => _camProvider?.Current ?? Camera.main;
    public float MoveSpeed => _movement.MoveSpeed;
    public float TurnSpeed => _movement.TurnSpeed;
    public bool IsGrounded() => _ground != null && _ground.CheckGround(Tf);
    public GroundCheckRuntime.TerrainType CurrentTerrain => _ground != null ? _ground.CurrentTerrain : GroundCheckRuntime.TerrainType.None;
    public float AttractMinDistance => _interactions ? _interactions.AttractMinDistance : 1f;
    public float AttractMaxDistance => _interactions ? _interactions.AttractMaxDistance : 5f;

    public AnimationCurve AttractSpeedCurve =>
        _interactions ? _interactions.AttractSpeedCurve : AnimationCurve.Linear(0, 1, 1, 1);

    public float AttractSpeedBase => _interactions ? _interactions.AttractSpeedBase : 1f;
    public float AimMaxDistance => _interactions.AimMaxDistance;
    public float AimMaxHeight => _interactions.AimMaxHeight;
    public float SmashRange => _interactions.smashRange;
    public LayerMask SmashLayer => _interactions.smashLayer;

    public Vector3 KnockbackTargetPosition;
    public float KnockbackDuration;
    public bool HasExternalImpact => Observer.HasKnockback;

    public Vector3 CameraRelativeDir(float h, float v)
    {
        var cam = Cam;
        Vector3 fwd = cam ? cam.transform.forward : Vector3.forward;
        Vector3 right = cam ? cam.transform.right : Vector3.right;
        fwd.y = 0f;
        right.y = 0f;
        fwd.Normalize();
        right.Normalize();
        return (fwd * v + right * h).normalized;
    }
    
    public bool TryGetPushTarget(out BoxPushAttract target, out RaycastHit left, out RaycastHit right)
    {
        target = null;
        left = default;
        right = default;
        return _interactions != null && _interactions.TryGetPushTarget(Tf, out target, out left, out right);
    }

    public bool TryGetAttractTarget(out BoxPushAttract target)
    {
        target = null;
        return _interactions != null && _interactions.TryGetAttractTarget(Tf, out target);
    }

    public bool TryGetSwingTarget(out Rigidbody hook)
    {
        hook = null;
        return _interactions != null && _interactions.TryGetSwingTarget(Tf, out hook);
    }

    public bool TryGetAim(Vector2 aimScreenPosition, out Vector3 pos, out Vector3 norm)
    {
        pos = default;
        norm = default;

        return _interactions != null && _interactions.TryGetAim(Tf, aimScreenPosition, out pos, out norm);
    }

    public bool IsAimValid => _interactions != null && _interactions.IsAimValid;

    public bool TryGetQuickTravel(Transform playerTf, out HippoTravel target)
    {
        target = null;
        return _interactions != null && _interactions.TryGetQuickTravel(Tf, out target);
    }
}