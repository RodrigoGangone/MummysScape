using UnityEngine;

public class Player : MonoBehaviour
{
    private Transform _cameraTransform;
    
    ModelPlayer _modelPlayer;
    ViewPlayer _viewPlayer;
    ControllerPlayer _controllerPlayer;

    private SpringJoint _springJoint;
    public AnimationCurve affectCurve;

    public StateMachinePlayer _StateMachinePlayer { get; private set; }
    private string _currentState;
    
    [SerializeField] private float _life;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxSpeed;
    private float _stockBandages;
    [SerializeField] private float _speedRotation ;
    
    public float Life { get => _life; set => _life = value; }
    public float Speed { get => _speed; set { } }
    public float SpeedRotation { get => _speedRotation; set => _speedRotation = value; }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _springJoint = GetComponent<SpringJoint>();
        
        _modelPlayer = new ModelPlayer(this, _springJoint);
        _controllerPlayer = new ControllerPlayer(_modelPlayer);
        _StateMachinePlayer = new StateMachinePlayer();
        
        _StateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle());
        _StateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot());
        _StateMachinePlayer.AddState(PlayerState.Walk, new SM_Walk());
        _StateMachinePlayer.AddState(PlayerState.Hook, new SM_Hook());
        _StateMachinePlayer.AddState(PlayerState.Grab, new SM_Grab());
        _StateMachinePlayer.AddState(PlayerState.Damage, new SM_Damage());
        _StateMachinePlayer.AddState(PlayerState.Dead, new SM_Dead());
    }

    
    private void Update()
    {
        _StateMachinePlayer.Update();
        _controllerPlayer.ControllerUpdate();
    }

    private void FixedUpdate()
    {
        _StateMachinePlayer.FixedUpdate();
    }

    #region StateMachineMetods
    
        void ChangeState(PlayerState playerState)
        {
            _StateMachinePlayer.ChangeState(playerState);
        }
    
        string CurrentState()
        {
            return _StateMachinePlayer.getCurrentState();
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
    
    #endregion
}
