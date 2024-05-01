using UnityEngine;

public class Player : MonoBehaviour
{ 
    ModelPlayer _modelPlayer;
    ViewPlayer _viewPlayer;
    ControllerPlayer _controllerPlayer;
    
    public Rigidbody _rigidbody { get; private set; }

    public Transform _cameraTransform { get; private set; }
    
    public SpringJoint _springJoint { get; private set; }
    public LineRenderer _bandage{ get; private set; }

    private StateMachinePlayer _stateMachinePlayer { get; set; }
    private string _currentState;

    [SerializeField] private float _life;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _speedRotation;
    
    public float Life { get => _life; set => _life = value; }
    public float Speed { get => _speed; set { } }
    public float SpeedRotation { get => _speedRotation; set => _speedRotation = value; }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _rigidbody = GetComponent<Rigidbody>();
        
        _springJoint = GetComponent<SpringJoint>();
        _bandage = GetComponent<LineRenderer>();

        _viewPlayer = new ViewPlayer(this);
        _modelPlayer = new ModelPlayer(this);
        _controllerPlayer = new ControllerPlayer(_modelPlayer, _viewPlayer);

        _stateMachinePlayer = new StateMachinePlayer();

        _stateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle());
        _stateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot());
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

    private enum PlayerState
    {
        Idle,
        Shoot,
        Walk,
        Hook,
        Grab,
        Damage,
        Dead
    }
}
