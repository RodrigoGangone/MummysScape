using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

public class Player : MonoBehaviour
{
    public ModelPlayer _modelPlayer { get; private set; }
    public ViewPlayer _viewPlayer { get; private set; }
    public ControllerPlayer _controllerPlayer { get; private set; }

    public Rigidbody _rigidbody { get; private set; }
    public Animator _anim { get; private set; }
    public Transform _cameraTransform { get; private set; }
    public SpringJoint _springJoint { get; private set; }
    public LineRenderer _bandage { get; private set; }

    public DetectionBeetle _detectionBeetle;

    public LevelManager levelManager;

    public StateMachinePlayer _stateMachinePlayer;

    private string _currentState;

    [Header("ATRIBUTES")][SerializeField] private float _life;
    [SerializeField] private float _speedOriginal = 5;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _speedRotation;

    [Header("BANDAGE")][SerializeField] private int _maxBandageStock = 2;
    [SerializeField] private int _minBandageStock = 0;
    [SerializeField] private int _currBandageStock = 2;
    [SerializeField] public Transform target;

    [Header("SIZES")][SerializeField] private PlayerSize _currentPlayerSize = PlayerSize.Normal;

    [SerializeField] public Mesh[] _Meshes;

    [Header("FXS")][SerializeField] public ParticleSystem _puffFX;

    //TODO: Mejorar esto a futuro

    #region Getters & Setters

    public float Life
    {
        get => _life;
        set => _life = value;
    }

    public float SpeedOriginal => _speedOriginal;

    public float Speed
    {
        get => _speed;
        set => _speed = value;
    }

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
        levelManager = FindObjectOfType<LevelManager>();

        _anim = GetComponentInChildren<Animator>();
        _detectionBeetle = GetComponentInChildren<DetectionBeetle>();

        _viewPlayer = new ViewPlayer(this);
        _modelPlayer = new ModelPlayer(this);
        _controllerPlayer = new ControllerPlayer(this);

        //Actions subscribe
        _controllerPlayer.OnGetCanShoot += CanShoot;
        _controllerPlayer.OnStateChange += ChangeState;
        _controllerPlayer.OnGetState += CurrentState;

        levelManager.playerWin += Win;
    }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _stateMachinePlayer = new StateMachinePlayer();

        _stateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Walk, new SM_Walk(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Hook, new SM_Hook(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Fall, new SM_Fall(_modelPlayer, _viewPlayer));
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

    public void ChangeSpeed()
    {
        switch (CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                _speed = 2;
                break;
            case PlayerSize.Small:
                _speed = 4;
                break;
            case PlayerSize.Head:
                _speed = _speedOriginal;
                break;
        }
    }

    void Win()
    {
        _viewPlayer.PLAY_ANIM_TRIGGER("Win");
        StartCoroutine(Disolve(5));
        enabled = false;
        //TODO: SETEAR MATERIAL
    }

    private IEnumerator Disolve(float t)
    {
        float tick = 0f;
        float value = 1;

        while (value > 0f)
        {
            Debug.Log(value);
            value = Mathf.Lerp(1f, 0f, tick);
            _viewPlayer.playerMat.SetFloat("_CutoffLight", value);
            tick += Time.deltaTime / t;
            yield return null;
        }

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
    Fall,
    Grab,
    Damage,
    Dead
}