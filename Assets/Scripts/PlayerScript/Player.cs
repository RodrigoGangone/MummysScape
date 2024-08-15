using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

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

    [SerializeField] public SkinnedMeshRenderer bodySM;

    [SerializeField] public SkinnedMeshRenderer headSM;

    public DetectionHook _detectionBeetle;

    public LevelManager levelManager;

    public StateMachinePlayer _stateMachinePlayer;

    private string _currentState;

    [Header("ATRIBUTES")] [SerializeField] private float _life;
    [SerializeField] private float _speedOriginal = 5;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _speedRotation;

    [Header("BANDAGE")] [SerializeField] private int _maxBandageStock = 2;
    [SerializeField] private int _minBandageStock = 0;
    [SerializeField] private int _currBandageStock = 2;
    [SerializeField] public Transform handTarget;
    [SerializeField] public Transform shootTarget;

    [Header("SIZES")] [SerializeField] private PlayerSize _currentPlayerSize = PlayerSize.Normal;
    [SerializeField] public Mesh[] _Meshes;

    [Header("FXS")] [SerializeField] public ParticleSystem _puffFX;
    [SerializeField] public ParticleSystem _walkFX;

    [FormerlySerializedAs("_rightHand")] [SerializeField]
    public TwoBoneIKConstraint rightHand;

    [SerializeField] public RigBuilder rigBuilder;
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

    private void OnDrawGizmos()
    {
        Vector3[] origin = new Vector3[]
        {
            shootTarget.transform.position,
            shootTarget.transform.position + new Vector3(0.25f, 0, 0),
            shootTarget.transform.position + new Vector3(0.50f, 0, 0),
            shootTarget.transform.position + new Vector3(-0.25f, 0, 0),
            shootTarget.transform.position + new Vector3(-0.50f, 0, 0),
        };

        foreach (var ori in origin)
        {
            RaycastHit hit;

            if (Physics.Raycast(ori, shootTarget.forward, out hit, 100f))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ori, hit.point);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
        }
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
        _detectionBeetle = GetComponentInChildren<DetectionHook>();

        _viewPlayer = new ViewPlayer(this, bodySM, headSM);
        _modelPlayer = new ModelPlayer(this);
        _controllerPlayer = new ControllerPlayer(this);

        //Actions subscribe
        _controllerPlayer.OnGetCanShoot += CanShoot;
        _controllerPlayer.OnStateChange += ChangeState;
        _controllerPlayer.OnGetState += CurrentState;

        levelManager.playerWin += Win;
        levelManager.playerDeath += Death;
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
        _stateMachinePlayer.AddState(PlayerState.Win, new SM_Win(this));
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
        _stateMachinePlayer.ChangeState(PlayerState.Win);
    }

    void Death()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("KillPlane"))
        {
            Debug.Log("Player murio por killPlane");
            Death();
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
    Win,
    Dead
}