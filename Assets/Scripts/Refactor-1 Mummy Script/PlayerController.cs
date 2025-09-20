using UnityEngine;
using static PlayerEnum;

/// <summary>
/// PlayerController
/// Orquesta todo: crea Model, bindea MovementRuntime, arma PlayerContext,
/// registra States en tu StateMachinePlayer y define el estado inicial.
/// Requiere Rigidbody y StateMachinePlayer en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(StateMachinePlayer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputStateDriver))]
[RequireComponent(typeof(GroundCheckRuntime))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Refs (Scene/Prefab)")]
    [SerializeField] private PlayerView _view;
    [SerializeField] private CameraProvider _cameraProvider;
    [SerializeField] private MovementRuntime _movement;
    [SerializeField] private PlayerInputReaderLegacy _input;  // podés cambiar por el de New Input System
    [SerializeField] private HeadTimerController _headTimer;  // opcional (puede estar en UI)
    [SerializeField] private InteractionRuntime _interactions;
    [SerializeField] private GroundCheckRuntime _ground;
    [SerializeField] private GameObject _bandagePickupPrefab; // opcional

    private StateMachinePlayer _sm;
    private PlayerInputStateDriver _inputDriver;
    private Rigidbody _rb;
    private PlayerModel _model;
    private PlayerContext _ctx;

    private void Awake()
    {
        _sm = GetComponent<StateMachinePlayer>();
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        _inputDriver = GetComponent<PlayerInputStateDriver>();

        // Model con 2 vendas (Normal)
        _model = new PlayerModel();

        // MovementRuntime escucha Size del Model
        _movement.Bind(_model);

        // View opcional: actualizar sprite del reloj cuando cambie el Size
        _model.OnSizeChanged += size => _view?.SetHeadTimerSprite(size == PlayerSize.Head);

        _headTimer?.Bind(_model);

        _ground = GetComponent<GroundCheckRuntime>();
        
        // Contexto compartido por States
        _ctx = new PlayerContext(transform, _rb, _cameraProvider, _model, _view, _movement, _input, _interactions, _ground);

        // Estados (tu API AddState / ChangeState)
        _sm.AddState(PlayerStateId.Idle,  new IdleState(_ctx));
        _sm.AddState(PlayerStateId.Walk,  new WalkState(_ctx));
        _sm.AddState(PlayerStateId.Fall, new FallState(_ctx));
        _sm.AddState(PlayerStateId.Shoot, new ShootState(_ctx));
        _sm.AddState(PlayerStateId.Smash, new SmashState(_ctx));
        _sm.AddState(PlayerStateId.DropBandage, new DropBandageState(_ctx, _bandagePickupPrefab));
        //_sm.AddState(PlayerStateId.Push, new PushState(_ctx));
        //_sm.AddState(PlayerStateId.Attract, new AttractState(_ctx));
        //_sm.AddState(PlayerStateId.Swing, new SwingState(_ctx));
        //_sm.AddState(PlayerStateId.Dead, new DeadState(_ctx));
        
        _sm.SetGuard(new PlayerTransitionGuard(_ctx));  // Guard central (usa TransitionRules + SizeRules)
        _inputDriver.Bind(_ctx, _sm);   // Driver que convierte inputs a estados
        _sm.ChangeState(PlayerStateId.Idle); // Estado inicial
    }
    
    // Intenta sumar 'amount' vendas al Model (retorna false si ya estás en el máximo).
    public bool TryCollectBandage(int amount)
    {
        int before = _model.Bandages;
        _model.AddBandages(amount);
        return _model.Bandages > before;
    }
    
    // Invocado por HeadTimer al expirar (UnityEvent): mata al player.</summary>
    public void Kill() => _sm.ChangeState(PlayerStateId.Dead);
}
