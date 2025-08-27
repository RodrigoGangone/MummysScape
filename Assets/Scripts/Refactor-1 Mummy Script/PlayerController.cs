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
public sealed class PlayerController : MonoBehaviour
{
    [Header("Refs (Scene/Prefab)")]
    [SerializeField] private PlayerView _view;
    [SerializeField] private CameraProvider _cameraProvider;
    [SerializeField] private MovementRuntime _movement;
    [SerializeField] private PlayerInputReaderLegacy _input;  // podés cambiar por el de New Input System
    [SerializeField] private HeadTimerController _headTimer;  // opcional (puede estar en UI)
    [SerializeField] private InteractionRuntime _interactions;
    [SerializeField] private GameObject _bandagePickupPrefab; // opcional

    private StateMachinePlayer _sm;
    private Rigidbody _rb;
    private PlayerModel _model;
    private PlayerContext _ctx;

    private void Awake()
    {
        _sm = GetComponent<StateMachinePlayer>();
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Model con 2 vendas (Normal)
        _model = new PlayerModel(PlayerModel.MaxBandages);

        // MovementRuntime escucha Size del Model
        _movement.Bind(_model);

        // View opcional: actualizar sprite del reloj cuando cambie el Size
        _model.OnSizeChanged += size => _view?.SetHeadTimerSprite(size == PlayerSize.Head);

        _headTimer?.Bind(_model);
        
        // Contexto compartido por States
        _ctx = new PlayerContext(transform, _rb, _cameraProvider, _model, _view, _movement, _input, _interactions);

        // Estados (tu API AddState / ChangeState)
        _sm.AddState(PlayerStateId.Idle,  new IdleState(_ctx));
        _sm.AddState(PlayerStateId.Walk,  new WalkState(_ctx));
        _sm.AddState(PlayerStateId.Shoot, new ShootState(_ctx));
        _sm.AddState(PlayerStateId.Smash, new SmashState(_ctx));
        _sm.AddState(PlayerStateId.Drop,    new DropBandageState(_ctx, _bandagePickupPrefab));
        _sm.AddState(PlayerStateId.Attract, new AttractState(_ctx, _interactions));

        _sm.ChangeState(PlayerStateId.Idle);
    }
    
    /// <summary>
    /// Intenta sumar 'amount' vendas al Model (retorna false si ya estás en el máximo).
    /// </summary>
    public bool TryCollectBandage(int amount)
    {
        int before = _model.Bandages;
        _model.AddBandages(amount);
        return _model.Bandages > before;
    }
}
