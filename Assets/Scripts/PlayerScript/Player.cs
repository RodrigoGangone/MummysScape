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

    [SerializeField] private float _life;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _speedRotation;

    [SerializeField] private int _maxNumOfShoot = 2;
    [SerializeField] private int _currNumOfShoot = 0;

    //Var para saber el tamaño del player.
    [SerializeField] private PlayerSize _currentPlayerSize = PlayerSize.Normal;

    //Los GameObjets con los 3 tamaños de la Mummy
    [SerializeField] public GameObject MummyNormal;
    [SerializeField] public GameObject MummySmall;
    [SerializeField] public GameObject MummyHead;

    public float Life
    {
        get => _life;
        set => _life = value;
    }

    public float Speed
    {
        get => _speed;
        set { }
    }

    public float SpeedRotation
    {
        get => _speedRotation;
        set => _speedRotation = value;
    }

    public int MaxNumOfShoot
    {
        get => _maxNumOfShoot;
        set { }
    }

    public int CurrentNumOfShoot
    {
        get => _currNumOfShoot;
        set => _currNumOfShoot = value;
    }
    
    public PlayerSize CurrentPlayerSize
    {
        get => _currentPlayerSize;
        set => _currentPlayerSize = value;
    }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _rigidbody = GetComponent<Rigidbody>();
        
        _springJoint = GetComponent<SpringJoint>();
        _bandage = GetComponent<LineRenderer>();
        
        _detectionBeetle = GetComponentInChildren<DetectionBeetle>();

        _viewPlayer = new ViewPlayer(this);
        _modelPlayer = new ModelPlayer(this);
        _controllerPlayer = new ControllerPlayer(this);

        _stateMachinePlayer = new StateMachinePlayer();

        _stateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle());
        _stateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot(this));
        _stateMachinePlayer.AddState(PlayerState.Walk, new SM_Walk());
        _stateMachinePlayer.AddState(PlayerState.Hook, new SM_Hook());
        _stateMachinePlayer.AddState(PlayerState.Grab, new SM_Grab());
        _stateMachinePlayer.AddState(PlayerState.Damage, new SM_Damage());
        _stateMachinePlayer.AddState(PlayerState.Dead, new SM_Dead());
    }

    private void Update()
    {
        _stateMachinePlayer.Update();
        _controllerPlayer.ControllerUpdate();
    }

    private void OnDrawGizmos()
    {
        // Guardar la posición y rotación del jugador
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // Calcular la posición del boxcast
        Vector3 boxcastPos = pos + rot * new Vector3(0, 2, 5f);

        // Calcular la rotación del boxcast
        Quaternion boxcastRot = rot;

        // Dibujar el boxcast
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(boxcastPos, boxcastRot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(5, 8, 10));
        Gizmos.matrix = Matrix4x4.identity; // Restaurar la matriz de gizmos a la identidad
    }

    private void FixedUpdate()
    {
        _stateMachinePlayer.FixedUpdate();
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