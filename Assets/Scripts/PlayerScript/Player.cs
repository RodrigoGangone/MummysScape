using UnityEngine;

public class Player : MonoBehaviour
{
    private Transform _cameraTransform;

    public ModelPlayer _modelPlayer { get; private set; }
    public ViewPlayer _viewPlayer { get; private set; }
    ControllerPlayer _controllerPlayer;
    
    private SpringJoint _springJoint;
    private LineRenderer _bandage;
    public StateMachinePlayer _StateMachinePlayer { get; private set; }
    private string _currentState;

    [SerializeField] private float _life;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _speedRotation;
    [SerializeField] public GameObject _bandagesPrefab;
    [SerializeField] public GameObject _indicatorShoot;

    private float _stockBandages;

    public float Life { get => _life; set => _life = value; }
    public float Speed { get => _speed; set { } }
    public float SpeedRotation { get => _speedRotation; set => _speedRotation = value; }

    private void Start()
    {
        if (Camera.main != null) _cameraTransform = Camera.main.transform;

        _springJoint = GetComponent<SpringJoint>();

        _viewPlayer = new ViewPlayer(this, _modelPlayer);
        _modelPlayer = new ModelPlayer(this, _springJoint, _viewPlayer);
        _controllerPlayer = new ControllerPlayer(this);

        _StateMachinePlayer = new StateMachinePlayer();

        _StateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle());
        _StateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot(this));
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
        _StateMachinePlayer.FixedUpdate();
        _controllerPlayer.ControllerFixedUpdate();
    }

    void ChangeState(PlayerState playerState)
    {
        _StateMachinePlayer.ChangeState(playerState);
    }

    string CurrentState()
    {
        return _StateMachinePlayer.getCurrentState();
    }

    
}
public enum PlayerState
{
    Idle,
    Shoot,
    Walk,
    Hook,
    Grab,
    Damage,
    Dead
}