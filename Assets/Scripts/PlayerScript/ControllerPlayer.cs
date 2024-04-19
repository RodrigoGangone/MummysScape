using Unity.VisualScripting;
using UnityEngine;

public class ControllerPlayer 
{
    ModelPlayer _model;
    float _rotationInput;
    float _moveInput;
    private bool _whatmovement = true;
    
    public ControllerPlayer(ModelPlayer m)
    {
        _model = m;
    }

    public void ControllerUpdate()
    {
        #region Mouse

        if(Input.GetKeyDown(KeyCode.Mouse0)) { _model.Aim(); }

        #endregion
        
    #region Move
        
        _rotationInput = Input.GetAxis("Horizontal");
        _moveInput = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Alpha1)) { _whatmovement = true; }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) { _whatmovement = false; }
        
        if (_rotationInput != 0 || _moveInput != 0 && _whatmovement) { _model.MoveVariant(_rotationInput, _moveInput); }
        if (_rotationInput != 0 || _moveInput != 0 && !_whatmovement) { _model.MoveTank(_rotationInput, _moveInput); }
        
    #endregion

    #region Skills

        #region Hook
            
            if (Input.GetKeyDown(KeyCode.Space)) { _model.Hook(); }
            if(_model.objectToHookUpdated) { _model.lineCurrent?.Invoke(); _model.limitVelocity?.Invoke(); }
            if (Input.GetKeyUp(KeyCode.Space)){ _model.reset?.Invoke(); }
            
        #endregion

    #endregion
        
    }
}
