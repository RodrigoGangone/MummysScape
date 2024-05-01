using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerPlayer
{
    ModelPlayer _model;
    ViewPlayer _view;
    Player _player;
    float _rotationInput;
    float _moveInput;
    private bool _whatmovement = true;
    private bool isMoveTank;

    public ControllerPlayer(Player player)
    {
        _model = player._modelPlayer;
        _view = player._viewPlayer;
        _player = player;
    }

    public void ControllerUpdate()
    {
        if (_model == null || _view == null) return;
        
        _rotationInput = Input.GetAxisRaw("Horizontal");
        
        _moveInput = Input.GetAxisRaw("Vertical");
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            _model.Aim();
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (isMoveTank)
                isMoveTank = false;
            else
                isMoveTank = true;
        }
        
        if (Input.GetKey(KeyCode.Space))
        {
            _model.HookBalanced();
        }

        if (_model.objectToHookUpdated)
        {
            _model.lineCurrent?.Invoke();
            _model.limitVelocity?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _model.reset?.Invoke();
        }

        if (Input.GetMouseButtonDown(0))
        {
            _model.CheckPick();
        }

        if (Input.GetMouseButton(0)) // Verifica si se mantiene presionado el clic izquierdo
        {
            _model.Pick();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _model.Drop();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            _player._StateMachinePlayer.ChangeState(PlayerState.Shoot);
        }
    }

    public void ControllerFixedUpdate()
    {
        if (_rotationInput != 0 || _moveInput != 0)
        {
            if (isMoveTank)
                _model.MoveTank(_rotationInput, _moveInput);
            else
                _model.MoveVariant(_rotationInput, _moveInput);
        }
    }
}