using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
    [SerializeField] private float _speedOriginal = 4;
    [SerializeField] private float _speedRotationOriginal = 12;
    [SerializeField] private float _speed = 4;
    [SerializeField] private float _speedRotation = 12;
    [SerializeField] private float _speedPush = 1f;
    [SerializeField] private float _speedHooked = 5;
    [SerializeField] private AnimationCurve _speedPull;

    [Header("BANDAGE")] [SerializeField] public GameObject _prefabBandage;
    [SerializeField] private int _maxBandageStock = 2;
    [SerializeField] private int _minBandageStock = 0;
    [SerializeField] private int _currBandageStock = 2;
    [SerializeField] public Transform handTarget;
    [SerializeField] private Transform _shootTarget;

    [Header("TACKLE")] [SerializeField] public SphereCollider tackle;

    [Header("SIZES")] [SerializeField] private PlayerSize _currentPlayerSize = PlayerSize.Normal;
    [SerializeField] public Mesh[] _Meshes;

    [Header("FXS")] [SerializeField] public ParticleSystem _puffFX;
    [SerializeField] public ParticleSystem _walkFX;
    [SerializeField] public ParticleSystem smashFX;
    [SerializeField] public ParticleSystem _walkSandFX;
    [SerializeField] public GameObject _fire;
    //[SerializeField] public ParticleSystem _smoke;

    [SerializeField] public TwoBoneIKConstraint rightHand;
    [SerializeField] public RigBuilder rigBuilder;
    [SerializeField] public GameObject flame;

    [Header("FIRE COLOR")] [ColorUsage(hdr: true, showAlpha: true)] [SerializeField]
    public Color _fireColorNormal;

    [ColorUsage(hdr: true, showAlpha: true)] [SerializeField]
    public Color _fireColorSmall;
    //[SerializeField] public Color _fireColorTiny;

    [Header("BC DROP")] [SerializeField] private Vector3 boxHalfExtents = new(0.45f, 0.9f, 0.45f);
    [SerializeField] private float maxDistance = 0.5f;
    [SerializeField] private LayerMask wallLayerMask;

    [Header("GIZMOS")] [SerializeField] public bool GizmoAutoShoot = true;
    [SerializeField] public bool GizmoWallShoot = true;
    [SerializeField] public bool GizmoWallDrop = true;
    [SerializeField] public bool GizmoPush = true;
    [SerializeField] public bool GizmoPull = true;

    //Actions

    public Action SizeModify;
    public bool WalkingSand;
    public bool IsHooked;

    //Rays
    private const float _rayCheckShootDistance = 1.5f;
    private const float _rayCheckPushDistance = 0.5f;
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

    public Vector3 BoxHalfExt => boxHalfExtents;
    public float MaxDistance => maxDistance;

    public float Speed => CurrentSpeed();

    public float SpeedPush => _speedPush;

    public float SpeedHook => _speedHooked;
    public AnimationCurve SpeedPull => _speedPull;

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
        _controllerPlayer.OnWalkingSand += () => WalkingSand;
        _controllerPlayer.OnHooked += () => IsHooked;

        levelManager.OnPlayerWin += Win;
        levelManager.OnPlayerDeath += Death;
    }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _stateMachinePlayer = new StateMachinePlayer();

        _stateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot(this));
        _stateMachinePlayer.AddState(PlayerState.Walk, new SM_Walk(this));
        _stateMachinePlayer.AddState(PlayerState.WalkSand, new SM_WalkSand(this));
        _stateMachinePlayer.AddState(PlayerState.Hook, new SM_Hook(this));
        _stateMachinePlayer.AddState(PlayerState.Fall, new SM_Fall(this));
        _stateMachinePlayer.AddState(PlayerState.Drop, new SM_Drop(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Push, new SM_Push(this));
        _stateMachinePlayer.AddState(PlayerState.Pull, new SM_Pull(this));
        _stateMachinePlayer.AddState(PlayerState.Smash, new SM_Smash(_modelPlayer, _viewPlayer));
        //_stateMachinePlayer.AddState(PlayerState.Damage, new SM_Damage(_modelPlayer, _viewPlayer));
        _stateMachinePlayer.AddState(PlayerState.Win, new SM_Win(this));
        _stateMachinePlayer.AddState(PlayerState.Dead, new SM_Dead(this));

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

    float CurrentSpeed()
    {
        switch (CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                _speed = _speedOriginal;
                break;
            case PlayerSize.Small:
                _speed = _speedOriginal;
                break;
            case PlayerSize.Head:
                _speed = _speedOriginal;
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
        _stateMachinePlayer.ChangeState(PlayerState.Dead);
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
            Vector3 origin = _shootTarget.transform.position;

            Quaternion leftRotation01 = Quaternion.Euler(0, -10, 0);
            Quaternion rightRotation01 = Quaternion.Euler(0, 10, 0);

            Quaternion leftRotation02 = Quaternion.Euler(0, -20, 0);
            Quaternion rightRotation02 = Quaternion.Euler(0, 20, 0);

            Quaternion leftRotation03 = Quaternion.Euler(0, -30, 0);
            Quaternion rightRotation03 = Quaternion.Euler(0, 30, 0);

            Vector3 leftDirection01 = leftRotation01 * transform.forward;
            Vector3 rightDirection01 = rightRotation01 * transform.forward;

            Vector3 leftDirection02 = leftRotation02 * transform.forward;
            Vector3 rightDirection02 = rightRotation02 * transform.forward;

            Vector3 leftDirection03 = leftRotation03 * transform.forward;
            Vector3 rightDirection03 = rightRotation03 * transform.forward;

            Vector3 centerDirection = transform.forward; // Rayo que va hacia adelante

            Vector3[] directions =
            {
                leftDirection01, rightDirection01, leftDirection02, leftDirection03, rightDirection02, rightDirection03,
                centerDirection
            };

            foreach (var direction in directions)
            {
                RaycastHit hit;

                if (Physics.Raycast(origin, direction, out hit, 15))
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(origin, hit.point);
                    Gizmos.DrawSphere(hit.point, 0.1f);
                }
            }
        }

        #endregion

        #region Evitar Shoot cerca de Wall

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

        #region Evitar Drop cerca de Wall

        if (GizmoWallDrop)
        {
            Vector3[] directions =
            {
                -transform.forward,
                transform.forward,
                transform.right,
                -transform.right
            };
            // Desfazes en base al jugador
            Vector3[] localOffsets =
            {
                -transform.forward * 0.65f + new Vector3(0, 1f, 0), // NO TOCAR ESTOS VALORES
                transform.forward * 0.65f + new Vector3(0, 1f, 0), // NO TOCAR ESTOS VALORES
                transform.right * 0.65f + new Vector3(0, 1f, 0), // NO TOCAR ESTOS VALORES
                -transform.right * 0.65f + new Vector3(0, 1f, 0) // NO TOCAR ESTOS VALORES
            };

            // Recorremos ambas listas de dirección y desplazamiento de origen
            for (int i = 0; i < directions.Length; i++)
            {
                PerformBoxCast(directions[i], localOffsets[i]);
            }
        }

        #endregion

        #region Check Push Box

        if (GizmoPush)
        {
            // Posición de origen del raycast
            var _rayCheckPushPos = new Vector3(
                transform.position.x,
                _shootTarget.transform.position.y,
                transform.position.z
            );

            // Definir la máscara de la capa MovableBox
            var movableBoxLayer = LayerMask.NameToLayer("MovableBox");
            var layerMaskBox = 1 << movableBoxLayer;

            // Inicialmente, todos los rayos serán de color rojo
            Gizmos.color = Color.red;

            // Raycast 0.25 unidades a la derecha
            Vector3 rightOffset = _rayCheckPushPos + transform.right * 0.15f;
            RaycastHit hitRight;
            bool hitBoxRight = Physics.Raycast(rightOffset, transform.forward, out hitRight,
                _rayCheckPushDistance, layerMaskBox);

            // Raycast 0.25 unidades a la izquierda
            Vector3 leftOffset = _rayCheckPushPos - transform.right * 0.15f;
            RaycastHit hitLeft;
            bool hitBoxLeft = Physics.Raycast(leftOffset, transform.forward, out hitLeft,
                _rayCheckPushDistance, layerMaskBox);

            // Contar si ambos rayos golpean el mismo objeto
            int hitCount = 0;
            string boxName = null;

            if (hitBoxRight)
            {
                boxName = hitRight.collider.gameObject.name;
                hitCount++;
            }

            if (hitBoxLeft && hitLeft.collider.gameObject.name == boxName)
            {
                hitCount++;
            }

            // Si ambos rayos golpean el mismo objeto, cambiar el color a verde
            if (hitCount == 2)
            {
                Gizmos.color = Color.green;
            }

            // Dibujar los rayos con el color final (rojo o verde)
            Gizmos.DrawRay(rightOffset, transform.forward * _rayCheckPushDistance);
            Gizmos.DrawSphere(rightOffset + transform.forward * _rayCheckPushDistance, 0.1f);

            Gizmos.DrawRay(leftOffset, transform.forward * _rayCheckPushDistance);
            Gizmos.DrawSphere(leftOffset + transform.forward * _rayCheckPushDistance, 0.1f);
        }

        #endregion

        #region Check Pull Box

        if (GizmoPull)
        {
            var _rayCheckPullPos = new Vector3(transform.position.x,
                _shootTarget.transform.position.y,
                transform.position.z);

            Vector3 forwardDirection = transform.forward;
            Vector3 rightDirection = Quaternion.Euler(0, 5, 0) * transform.forward;
            Vector3 leftDirection = Quaternion.Euler(0, -5, 0) * transform.forward;

            Vector3[] directions = { forwardDirection, rightDirection, leftDirection };

            foreach (var direction in directions)
            {
                RaycastHit hit;

                if (Physics.Raycast(_rayCheckPullPos, direction, out hit, _rayCheckPullDistance))
                {
                    if (_modelPlayer != null)
                        Gizmos.color = _modelPlayer.CanPullBox() ? Color.yellow : Color.blue;

                    Gizmos.DrawLine(_rayCheckPullPos, hit.point);
                    Gizmos.DrawSphere(hit.point, 0.1f);
                }
                else
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawRay(_rayCheckPullPos, direction * _rayCheckPullDistance);
                    Gizmos.DrawSphere(_rayCheckPullPos + direction * _rayCheckPullDistance, 0.1f);
                }
            }
        }

        #endregion
    }

    void PerformBoxCast(Vector3 direction, Vector3 localOffsets)
    {
        Vector3 origin = transform.position + localOffsets;
        Quaternion orientation = transform.rotation;

        RaycastHit[] hits = Physics.BoxCastAll(
            origin,
            boxHalfExtents,
            direction,
            orientation,
            maxDistance,
            wallLayerMask
        );

        bool isTouchingWall = false;
        foreach (var hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & wallLayerMask) != 0) // Verif si es "Wall"
            {
                isTouchingWall = true;
                break;
            }
        }

        Gizmos.color = isTouchingWall ? Color.red : Color.green;

        if (isTouchingWall)
        {
            Gizmos.DrawWireCube(origin + direction * maxDistance, boxHalfExtents * 2);
        }
        else
        {
            Gizmos.DrawRay(origin, direction * maxDistance);
            Gizmos.DrawWireCube(origin + direction * maxDistance, boxHalfExtents * 2);
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
    WalkSand,
    Hook,
    Fall,
    Push,
    Pull,
    Smash,
    Drop,
    Damage,
    Win,
    Dead
}