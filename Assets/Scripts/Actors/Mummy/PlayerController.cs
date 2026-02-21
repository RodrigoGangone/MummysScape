using UnityEngine;
using static PlayerEnum;

/// <summary> 
/// Orquestador del Jugador: Centraliza la inicialización del modelo, la configuración de la FSM y 
/// el bindeo de todos los sistemas de soporte (físicas, inputs y visuales). 
/// </summary>

[RequireComponent(typeof(StateMachinePlayer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputStateDriver))]
[RequireComponent(typeof(GroundCheckRuntime))]
public sealed class  PlayerController : MonoBehaviour, IPausable, ILocked
{
    [Header("Refs (Scene/Prefab)")] [SerializeField]
    private PlayerView _view;

    [SerializeField] private CameraProvider _cameraProvider;
    [SerializeField] private MovementRuntime _movement;
    [SerializeField] private SwingHandler _swingHandler;
    [SerializeField] private EnvironmentObserver _observer;
    [SerializeField] private PlayerInputReaderLegacy _input; 
    [SerializeField] private HeadTimerController _headTimer; 
    [SerializeField] private InteractionRuntime _interactions;
    [SerializeField] private GroundCheckRuntime _ground;
    [SerializeField] private GameObject _bandagePickupPrefab;

    [Header("Size Runtime")] [SerializeField]
    private PlayerSizeVisual _sizeVisual;

    [SerializeField] private CapsuleBySizeRuntime _capsuleBySize;

    private StateMachinePlayer _sm;
    private PlayerInputStateDriver _inputDriver;
    private Rigidbody _rb;
    private PlayerModel _model;
    private PlayerContext _ctx;

    public PlayerContext Ctx => _ctx;
    public PlayerSizeVisual SizeVisual => _sizeVisual;

    private void Awake()
    {
        _sm = GetComponent<StateMachinePlayer>();
        _rb = GetComponent<Rigidbody>();

        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        _swingHandler = GetComponent<SwingHandler>();
        _inputDriver = GetComponent<PlayerInputStateDriver>();


        var pe = GameEventManager.Instance.playerEvents;

        _model = new PlayerModel(pe.OnBandagesCountChanged, pe.OnSizeChanged);

        _movement.Bind(_model);
        
        _headTimer?.Bind(_model);
        _sizeVisual?.Bind(_model);
        _capsuleBySize?.Bind(_model);

        _ground = GetComponent<GroundCheckRuntime>();

        _ctx = new PlayerContext(transform, _rb, _swingHandler, _observer, _cameraProvider, _model, _view, _movement,
            _input,
            _interactions, _ground, _sm);

        _sm.AddState(PlayerStateId.Idle, new IdleState(_ctx));
        _sm.AddState(PlayerStateId.Walk, new WalkState(_ctx));
        _sm.AddState(PlayerStateId.Fall, new FallState(_ctx));
        _sm.AddState(PlayerStateId.Aim, new AimState(_ctx));
        _sm.AddState(PlayerStateId.Shoot, new ShootState(_ctx));
        _sm.AddState(PlayerStateId.Smash, new SmashState(_ctx));
        _sm.AddState(PlayerStateId.DropBandage, new DropBandageState(_ctx, _bandagePickupPrefab));
        _sm.AddState(PlayerStateId.Push, new PushState(_ctx));
        _sm.AddState(PlayerStateId.Attract, new AttractState(_ctx));
        _sm.AddState(PlayerStateId.Swing, new SwingState(_ctx));
        _sm.AddState(PlayerStateId.QuickTravel, new QuickTravelState(_ctx));
        _sm.AddState(PlayerStateId.KnockBack, new KnockBackState(_ctx, _bandagePickupPrefab));
        _sm.AddState(PlayerStateId.Dead, new DeadState(_ctx));
        _sm.AddState(PlayerStateId.Win, new WinState(_ctx));

        _sm.SetGuard(new PlayerTransitionGuard(_ctx)); 
        _inputDriver.Bind(_ctx, _sm); 
        _sm.ChangeState(PlayerStateId.Idle); 

        pe.OnBandagesCountChanged.Raise(_model.Bandages);
        pe.OnSizeChanged.Raise(_model.Size);
    }

    public bool TryCollectBandage(int amount)
    {
        if (_sm.CurrentStateImplement<IBandageRestrictor>())
        {
            return false;
        }

        int before = _model.Bandages;
        _model.AddBandages(amount);
        return _model.Bandages > before;
    }

    private void Kill() => _sm.ChangeState(PlayerStateId.Dead);
    private void Win() => _sm.ChangeState(PlayerStateId.Win);

    public void OnPauseChanged(bool paused)
    {
        PlayerControlState.SetPause(paused);

        _rb.isKinematic = PlayerControlState.AnyBlocked;

        if (paused && (_sm.IsCurrent(PlayerStateId.Aim) || _sm.IsCurrent(PlayerStateId.Shoot)))
            _sm.ChangeState(PlayerStateId.Idle);

        _sm.enabled = !paused;
    }
    
    public void OnLockChanged(bool locked)
    {
        PlayerControlState.SetLock(locked);

        _rb.isKinematic = PlayerControlState.AnyBlocked;

        if (locked && (_sm.IsCurrent(PlayerStateId.Aim) || _sm.IsCurrent(PlayerStateId.Shoot)))
            _sm.ChangeState(PlayerStateId.Idle);

        _sm.enabled = !locked;
        GetComponent<Collider>().enabled = !locked;
    }

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
        GameEventManager.Instance.levelEvents.OnDeath.Register(Kill);
        GameEventManager.Instance.playerEvents.OnWin.Register(Win);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Kill);
        GameEventManager.Instance.levelEvents.OnWin.Unregister(Win);
    }
}