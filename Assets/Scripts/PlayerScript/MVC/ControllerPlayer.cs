using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utils;
using Debug = UnityEngine.Debug;

public class ControllerPlayer
{
    private Player _player;
    private ModelPlayer _model;
    private bool isSmashAnimation;

    private float _rotationInput;
    private float _moveInput;

    //Actions
    public event Func<bool> OnGetCanShoot;
    public event Action<PlayerState> OnStateChange = delegate { };
    public event Func<string> OnGetState = () => "ERROR OnGetState (Controller Player)";
    public event Func<PlayerSize> OnGetPlayerSize;
    public event Func<bool> OnWalkingSand;
    public event Func<bool> OnHooked;

    public ControllerPlayer(Player player)
    {
        _model = player._modelPlayer;
    }

    public void ControllerUpdate()
    {
        Debug.Log("STATE ACTUAL " + OnGetState.Invoke());

        if (CanWalkState())
        {
            OnStateChange(OnWalkingSand!.Invoke() ? PlayerState.WalkSand : PlayerState.Walk);
        }

        if (CanPushState())
        {
            OnStateChange(PlayerState.Push);
        }

        if (CanIdleState())
        {
            OnStateChange(PlayerState.Idle);
        }

        if (Input.GetKey(KeyCode.Space))
        {
            if (CanPullState())
            {
                OnStateChange(PlayerState.Pull);
            }
            else if (CanHookState())
            {
                OnStateChange(PlayerState.Hook);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && CanSmashState())
        {
            OnStateChange(PlayerState.Smash);
        }

        if (CanShootState() && Input.GetKeyDown(KeyCode.E))
        {
            if (OnGetCanShoot.Invoke())
                OnStateChange(PlayerState.Shoot);
        }

        if (CanDropState() && Input.GetKeyDown(KeyCode.Q))
        {
            if (OnGetCanShoot.Invoke())
                OnStateChange(PlayerState.Drop);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void ControllerFixedUpdate()
    {
    }

    private bool IsWalking()
    {
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }

    private bool CanIdleState()
    {
        return !IsWalking() &&
               OnGetState?.Invoke() switch
               {
                   STATE_SHOOT => true,
                   STATE_WALK => true,
                   STATE_WALK_SAND => true,
                   STATE_FALL => true, // Averiguar cuando toca el suelo para pasarlo a idle
                   STATE_PUSH => true,
                   STATE_DAMAGE => true,
                   STATE_DROP => true,
                   STATE_SMASH => true,
                   NO_STATE => true,
                   _ => false
               };
    }

    private bool CanWalkState()
    {
        return IsWalking() &&
               !_model.CanPushBox() &&
               OnGetState?.Invoke() switch
               {
                   STATE_IDLE => true,
                   STATE_WALK_SAND => true,
                   STATE_WALK => true,
                   STATE_PUSH => true,
                   STATE_SMASH => true,
                   _ => false
               };
    }

    private bool CanPushState()
    {
        return PlayerSize.Normal.Equals(OnGetPlayerSize.Invoke()) &&
               _model.CanPushBox() &&
               OnGetState?.Invoke() switch
               {
                   STATE_IDLE => true,
                   STATE_WALK => true,
                   STATE_WALK_SAND => true,
                   _ => false
               };
    }

    private bool CanPullState()
    {
        return PlayerSize.Normal.Equals(OnGetPlayerSize.Invoke()) &&
               _model.CanPullBox() &&
               OnGetState?.Invoke() switch
               {
                   STATE_IDLE => true,
                   STATE_WALK => true,
                   STATE_WALK_SAND => true,
                   _ => false
               };
    }

    private bool CanShootState()
    {
        return !_model.IsTouchingWall() &&
               OnGetState?.Invoke() switch
               {
                   STATE_IDLE => true,
                   STATE_WALK => true,
                   STATE_WALK_SAND => true,
                   STATE_HOOK => true,
                   STATE_FALL => true,
                   NO_STATE => true,
                   _ => false
               };
    }

    private bool CanHookState()
    {
        return PlayerSize.Small.Equals(OnGetPlayerSize.Invoke()) &&
               _model.detectionBeetle.currentHook != null &&
               !OnHooked!.Invoke() &&
               OnGetState?.Invoke() switch
               {
                   STATE_IDLE => true,
                   STATE_SHOOT => true,
                   STATE_WALK => true,
                   STATE_WALK_SAND => true,
                   STATE_FALL => true,
                   _ => false
               };
    }

    private bool CanDropState() //TODO: Modificar estoy por CanDropBandage
    {
        return !PlayerSize.Head.Equals(OnGetPlayerSize.Invoke()) &&
               _model.CanDropBandage() &&
               OnGetState?.Invoke() switch
               {
                   STATE_IDLE => true,
                   STATE_WALK => true,
                   STATE_WALK_SAND => true,
                   _ => false
               };
    }

    private bool CanSmashState()
    {
        return PlayerSize.Head.Equals(OnGetPlayerSize.Invoke()) &&
               OnGetState?.Invoke() switch
               {
                   STATE_IDLE => true,
                   STATE_WALK => true,
                   STATE_WALK_SAND => true,
                   _ => false
               };
    }
}