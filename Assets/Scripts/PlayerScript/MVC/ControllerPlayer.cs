using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerPlayer
{
    private ModelPlayer _model;

    private float _rotationInput;
    private float _moveInput;
    
    //Actions
    public event Func<bool> OnGetCanShoot;
    public event Func<bool> HasGrabObj; 
    public event Action<PlayerState> OnStateChange = delegate { };
    public event Func<string> OnGetState = () => "ERROR OnGetState (Controller Player)";
    public event Func<PlayerSize> OnGetPlayerSize;

    public ControllerPlayer(Player player)
    {
        _model = player._modelPlayer;
    }

    public void ControllerUpdate()
    {
        if (CanWalkState() && IsWalking())
        {
            OnStateChange(PlayerState.Walk);
        }

        if (CanIdleState() && !IsWalking())
        {
            OnStateChange(PlayerState.Idle);
        }

        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("CanGrabState() =" + CanGrabState());
            Debug.Log("CanHookState() =" + CanHookState());
            
            if (CanGrabState())
            {
                OnStateChange(PlayerState.Grab);
            }
            else if (CanHookState())
            {
                OnStateChange(PlayerState.Hook);
            }
        }

        if (CanShootState() && !_model.IsTouchingWall() && Input.GetKeyDown(KeyCode.E))
        {
            if (OnGetCanShoot.Invoke())
                OnStateChange(PlayerState.Shoot);
        }

        if (CanDropState() && !_model.IsTouchingWall() && Input.GetKeyDown(KeyCode.Q))
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
        return OnGetState?.Invoke() switch
        {
            "SM_Idle" => false, // se puede borrar porque no se pisa//
            "SM_Shoot" => true,
            "SM_Walk" => true,
            "SM_Hook" => false,
            "SM_Fall" => true, // Averiguar cuando toca el suelo para pasarlo a idle
            "SM_Grab" => true,
            "SM_Damage" => true,
            "SM_Win" => false,
            "SM_Dead" => false,
            "No hay estado" => true,
        };
    }

    private bool CanWalkState()
    {
        return OnGetState?.Invoke() switch
        {
            "SM_Idle" => true,
            "SM_Shoot" => false,
            "SM_Walk" => false, // se puede borrar porque no se pisa//
            "SM_Hook" => false,
            "SM_Fall" => false, //Averiguar cuando toca el suelo para cambiar a idle o walk
            "SM_Grab" => true,
            "SM_Damage" => false,
            "SM_Win" => false,
            "SM_Dead" => false,
        };
    }

    private bool CanShootState()
    {
        return OnGetState?.Invoke() switch
        {
            "SM_Idle" => true,
            "SM_Shoot" => false, // se puede borrar porque no se pisa//
            "SM_Walk" => true,
            "SM_Hook" => true,
            "SM_Fall" => true,
            "SM_Grab" => false,
            "SM_Damage" => false,
            "SM_Win" => false,
            "SM_Dead" => false,
            "No hay estado" => true,
        };
    }

    private bool CanHookState()
    {
        return PlayerSize.Small.Equals(OnGetPlayerSize.Invoke()) && 
               _model.detectionBeetle.currentHook != null &&
               OnGetState?.Invoke() switch
        {
            "SM_Idle" => true,
            "SM_Shoot" => true,
            "SM_Walk" => true,
            "SM_Hook" => false,
            "SM_Fall" => true,
            "SM_Grab" => false,
            "SM_Damage" => false,
            "SM_Win" => false,
            "SM_Dead" => false,
        };
    }

    private bool CanDropState()
    {
        return OnGetState?.Invoke() switch
        {
            "SM_Idle" => true,
            "SM_Shoot" => false,
            "SM_Walk" => false,
            "SM_Hook" => false,
            "SM_Fall" => false,
            "SM_Grab" => true, //Ver que hacer con el grab ya que la animacion seria otra
            "SM_Drop" => false,
            "SM_Damage" => false,
            "SM_Win" => false,
            "SM_Dead" => false,
        };
    }
    
    private bool CanGrabState()
    {
        return HasGrabObj() &&
               PlayerSize.Normal.Equals(OnGetPlayerSize.Invoke()) &&
               OnGetState?.Invoke() switch
        {
            "SM_Idle" => true,
            "SM_Shoot" => false,
            "SM_Walk" => true,
            "SM_Hook" => false,
            "SM_Fall" => false,
            "SM_Grab" => false,
            "SM_Damage" => false,
            "SM_Win" => false,
            "SM_Dead" => false,
        }; 
    }
}