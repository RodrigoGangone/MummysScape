using UnityEngine;
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
    [SerializeField] public MeshRenderer fireMR;

    public DetectionHook _detectionBeetle;
    public LevelManager levelManager;
    public StateMachinePlayer _stateMachinePlayer;

    private string _currentState;

    [Header("ATRIBUTES")] [SerializeField] private float _life;
    [SerializeField] private float _speedOriginal;
    [SerializeField] private float _speedRotationOriginal;
    [SerializeField] private float _speed;
    [SerializeField] private float _speedRotation;
    [SerializeField] private float _speedPush = 0.5f;
    [SerializeField] private float _speedPull = 0.5f;

    [Header("BANDAGE")] [SerializeField] public GameObject _prefabBandage;
    [SerializeField] private int _maxBandageStock = 2;
    [SerializeField] private int _minBandageStock = 0;
    [SerializeField] private int _currBandageStock = 2;
    [SerializeField] public Transform handTarget;
    [SerializeField] private Transform _shootTarget;
    [SerializeField] public Transform dropTarget;

    [Header("SIZES")] 
    [SerializeField] private PlayerSize _currentPlayerSize = PlayerSize.Normal;
    [SerializeField] public Mesh[] _Meshes;

    [Header("FXS")] [SerializeField] public ParticleSystem _puffFX;
    [SerializeField] public ParticleSystem _walkFX;
    [SerializeField] public TwoBoneIKConstraint rightHand;
    [SerializeField] public RigBuilder rigBuilder;

    [Header("GIZMOS")] 
    [SerializeField] public bool GizmoAutoShoot = true;
    [SerializeField] public bool GizmoWallShoot = true;
    [SerializeField] public bool GizmoPush = true;
    [SerializeField] public bool GizmoPull = true;
    
    //Rays
    private const float _rayCheckShootDistance = 1.5f;
    private const float _rayCheckPushDistance = 0.75f;
    private const float _rayCheckPullDistance = 7f;

    //TODO: Mejorar esto a futuro

    #region Getters & Setters

    public Transform ShootTargetTransform => _shootTarget.transform;

    public float RayCheckShootDistance => _rayCheckShootDistance;
    public float RayCheckPushDistance => _rayCheckPushDistance;
    public float RayCheckPullDistance => _rayCheckPullDistance;

    public float Life
    {
        get => _life;
        set => _life = value;
    }
    
    public float Speed => CurrentSpeed();

    public float SpeedPush => _speedPush;
    public float SpeedPull => _speedPull;
    
    public float SpeedRotation => CurrentRotation();

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

        _viewPlayer = new ViewPlayer(this, bodySM, headSM, fireMR);
        _modelPlayer = new ModelPlayer(this);
        _controllerPlayer = new ControllerPlayer(this);

        //Actions subscribe
        _controllerPlayer.OnGetCanShoot += CanShoot;
        _controllerPlayer.OnStateChange += ChangeState;
        _controllerPlayer.OnGetState += CurrentState;
        _controllerPlayer.OnGetPlayerSize += () => CurrentPlayerSize;
        /*_controllerPlayer.IsPushingObj += () => IsPushingObj;*/

        _modelPlayer.CreateBandage += CreateBandage;

        levelManager.OnPlayerWin += Win;
        levelManager.OnPlayerDeath += Death;
    }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _stateMachinePlayer = new StateMachinePlayer();

        _stateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Walk, new SM_Walk(this));
        _stateMachinePlayer.AddState(PlayerState.Hook, new SM_Hook(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Fall, new SM_Fall(this));
        _stateMachinePlayer.AddState(PlayerState.Drop, new SM_Drop(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Push, new SM_Push(this));
        _stateMachinePlayer.AddState(PlayerState.Pull, new SM_Pull(_modelPlayer, _viewPlayer));
        //_stateMachinePlayer.AddState(PlayerState.Damage, new SM_Damage());
        _stateMachinePlayer.AddState(PlayerState.Win, new SM_Win(this));
        //_stateMachinePlayer.AddState(PlayerState.Dead, new SM_Dead());

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
    
    GameObject CreateBandage(Transform trans)
    {
        return Instantiate(_prefabBandage, trans.position, trans.rotation);
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

    float CurrentSpeed()
    {
        switch (CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                _speed = _speedOriginal;
                break;
            case PlayerSize.Small:
                _speed = _speedOriginal * 1.5f;
                break;
            case PlayerSize.Head:
                _speed = _speedOriginal * 1.25f;
                break;
        }

        return _speed;
    }

    float CurrentRotation()
    {
        switch (CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                _speedRotation = _speedRotationOriginal;
                break;
            case PlayerSize.Small:
                _speedRotation = _speedRotationOriginal;
                break;
            case PlayerSize.Head:
                _speedRotation = _speedRotationOriginal;
                break;
        }

        return _speedRotation;
    }

    void Win()
    {
        _stateMachinePlayer.ChangeState(PlayerState.Win);
    }

    void Death()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("KillPlane"))
            levelManager.OnPlayerDeath?.Invoke();
    }

    void OnDrawGizmos()
    {
        #region Auto Apunte Boton
        if (GizmoAutoShoot)
        {
            Vector3[] origins =
            {
                _shootTarget.transform.position + transform.right * 0.75f,
                _shootTarget.transform.position - transform.right * 0.75f,
            };

            foreach (var origin in origins)
            {
                RaycastHit hit;

                if (Physics.Raycast(origin, transform.forward, out hit, 12))
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(origin, hit.point);
                    Gizmos.DrawSphere(hit.point, 0.1f);
                }
            }
        }
        #endregion     
        
        #region Evitar disparar/drop cerca de Wall
        if (GizmoWallShoot)
        {
            if (_modelPlayer != null)
                Gizmos.color = _modelPlayer.IsTouchingWall() ? Color.red : Color.green;
            else
                Gizmos.color = Color.black;
            
            var _rayCheckShootPos = new Vector3(transform.position.x,
                _shootTarget.transform.position.y,
                transform.position.z);
            
            
            // Solo detectar la capa "Wall"
            int wallLayer = LayerMask.NameToLayer("Wall");
            int layerMaskWall = 1 << wallLayer;
            
            Gizmos.DrawRay(_rayCheckShootPos, transform.forward * _rayCheckShootDistance);
            Gizmos.DrawSphere(_rayCheckShootPos + transform.forward * _rayCheckShootDistance, 0.1f);
        }
        #endregion
        
        #region Check Push Box
        if (GizmoPush)
        {
            if (_modelPlayer != null)
                Gizmos.color = _modelPlayer.CanPushBox() ? Color.red : Color.cyan;
            else
                Gizmos.color = Color.black;
            
            var _rayCheckPushPos = new Vector3(transform.position.x,
                _shootTarget.transform.position.y,
                transform.position.z);
            
            Gizmos.DrawRay(_rayCheckPushPos, transform.forward * _rayCheckPushDistance);
            Gizmos.DrawSphere(_rayCheckPushPos + transform.forward * _rayCheckPushDistance, 0.1f);
        }
        #endregion
        
        #region Check Pull Box
        if (GizmoPull)
        {
            var _rayCheckPullPos = new Vector3(transform.position.x,
                _shootTarget.transform.position.y,
                transform.position.z);
            
            RaycastHit hit;

            if (Physics.Raycast(_rayCheckPullPos, transform.forward, out hit, _rayCheckPullDistance))
            {
                if (_modelPlayer != null)
                    Gizmos.color = _modelPlayer.CanPullBox() ? Color.yellow : Color.blue;
                
                Gizmos.DrawLine(_rayCheckPullPos, hit.point);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
            else
            {
                Gizmos.color = Color.black;
                Gizmos.DrawRay(_rayCheckPullPos, transform.forward * _rayCheckPullDistance);
                Gizmos.DrawSphere(_rayCheckPullPos + transform.forward * _rayCheckPullDistance, 0.1f);
            }
        }
        #endregion
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
    Hook,
    Fall,
    Push,
    Pull,
    Drop,
    Damage,
    Win,
    Dead
}