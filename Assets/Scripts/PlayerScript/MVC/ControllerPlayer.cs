using UnityEngine;
using UnityEngine.SceneManagement;

public class ControllerPlayer
{
    ModelPlayer _model;
    ViewPlayer _view;
    float _rotationInput;
    float _moveInput;
    private bool _whatmovement = true;
    private bool isMoveTank;
    
    //Obj picked vars//
    float _rotationInputObj;
    float _moveInputObj;

    public ControllerPlayer(ModelPlayer m, ViewPlayer v)
    {
        _model = m;
        _view = v;
    }

    public void ControllerUpdate()
    {
        if (_model == null || _view == null) return;
        
        _rotationInput = Input.GetAxisRaw("Horizontal");
        _moveInput = Input.GetAxisRaw("Vertical");

        if (_model.hasObject)
        {
            _rotationInputObj = Input.GetAxisRaw("Object Horizontal");
            _moveInputObj = Input.GetAxisRaw("Object Vertical");
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
            _model.Hook();
        }

        if (_model.objectToHookUpdated)
        {
            _model.lineCurrent?.Invoke();
            _model.limitVelocity?.Invoke();
            _model.jointPreferences?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _model.reset?.Invoke();
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!_model.hasObject)
                _model.PickObject();
            else
                _model.DropObject();
        }
    }

    public void ControllerFixedUpdate()
    {
        //Movimiento del player
        if (_rotationInput != 0 || _moveInput != 0)
        {
            if (isMoveTank)
                _model.MoveTank(_rotationInput, _moveInput);
            else
                _model.MoveVariant(_rotationInput, _moveInput);
        }

        //Movimiento del objeto
        if (_rotationInputObj != 0 || _moveInputObj != 0)
        {
            _model.MoveObject(_rotationInputObj, _moveInputObj);
        }
    }
}