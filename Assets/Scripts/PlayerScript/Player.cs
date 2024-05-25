using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    public ModelPlayer _modelPlayer { get; private set; }
    public ViewPlayer _viewPlayer { get; private set; }
    public ControllerPlayer _controllerPlayer { get; private set; }

    public Rigidbody _rigidbody { get; private set; }

    public Transform _cameraTransform { get; private set; }

    public SpringJoint _springJoint { get; private set; }
    public LineRenderer _bandage { get; private set; }

    public DetectionBeetle _detectionBeetle;

    public StateMachinePlayer _stateMachinePlayer;

    private string _currentState;

    [Header("ATRIBUTES")]
    [SerializeField] private float _life;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _speedRotation;

    [Header("BANDAGE")]
    [SerializeField] private int _maxBandageStock = 2;
    [SerializeField] private int _minBandageStock = 0;
    [SerializeField] private int _currBandageStock = 2;

    [Header("SIZES")]
    [SerializeField] private PlayerSize _currentPlayerSize = PlayerSize.Normal;
    
    [SerializeField] public GameObject MummyNormal;
    [SerializeField] public GameObject MummySmall;
    [SerializeField] public GameObject MummyHead;
    
    [Header("FXS")]
    [SerializeField] public ParticleSystem _puffFX;

    //TODO: Mejorar esto a futuro

    #region Getters & Setters
    public float Life
    {
        get => _life;
        set => _life = value;
    }

    public float Speed => _speed;

    public float SpeedRotation
    {
        get => _speedRotation;
        set => _speedRotation = value;
    }

    public int MaxBandageStock => _maxBandageStock;
    public int MinBandageStock => _minBandageStock;

    public int CurrentBandageStock
    {
        get => _currBandageStock;
        set => _currBandageStock = value;
    }

    public PlayerSize CurrentPlayerSize
    {
        get => _currentPlayerSize;
        set => _currentPlayerSize = value;
    }

    #endregion

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _springJoint = GetComponent<SpringJoint>();
        _bandage = GetComponent<LineRenderer>();
        _detectionBeetle = GetComponentInChildren<DetectionBeetle>();

        _viewPlayer = new ViewPlayer(this);
        _modelPlayer = new ModelPlayer(this);
        _controllerPlayer = new ControllerPlayer(this);
        
        //Actions subscribe
        _controllerPlayer.OnGetCanShoot += CanShoot;
        _controllerPlayer.OnStateChange += ChangeState;
        _controllerPlayer.OnGetState += CurrentState;
    }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _stateMachinePlayer = new StateMachinePlayer();

        _stateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle());
        _stateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Walk, new SM_Walk(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Hook, new SM_Hook(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Grab, new SM_Grab());
        _stateMachinePlayer.AddState(PlayerState.Damage, new SM_Damage());
        _stateMachinePlayer.AddState(PlayerState.Dead, new SM_Dead());
        
        _stateMachinePlayer.ChangeState(PlayerState.Idle);
    }

    private void Update()
    {
        _stateMachinePlayer?.Update();
        _controllerPlayer.ControllerUpdate();
    }

    private void FixedUpdate()
    {
        _stateMachinePlayer?.FixedUpdate();
        _controllerPlayer.ControllerFixedUpdate();
    }

    void ChangeState(PlayerState playerState)
    {
        _stateMachinePlayer.ChangeState(playerState);
    }

    string CurrentState()
    {
        return _stateMachinePlayer.getCurrentState();
    }
    
    bool CanShoot()
    {
        Debug.Log("Bandage Stock : " + _currBandageStock);
        return _currBandageStock > _minBandageStock;
    }
}

public enum PlayerSize
{
    Normal,
    Small,
    Head
}

public enum PlayerState
{
    Idle,
    Shoot,
    Walk,
    Head,
    Hook,
    Grab,
    Damage,
    Dead
}