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

    public ControllerPlayer(ModelPlayer m, ViewPlayer v)
    {
        _model = m;
        _view = v;
    }

    public void ControllerUpdate()
    {
        if (_model == null || _view == null) return;
        
    #region Restart Level
        if (Input.GetKeyDown(KeyCode.R)) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    #endregion
        
    #region Mouse
    
        if(Input.GetKeyDown(KeyCode.Mouse0)) { _model.Aim();}
        
    #endregion
        
    #region Move
        _rotationInput = Input.GetAxis("Horizontal");
        _moveInput = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (isMoveTank)
                isMoveTank = false;
            else
                isMoveTank = true;
        }

        if (_rotationInput != 0 || _moveInput != 0)
        {
            if (isMoveTank)
                _model.MoveTank(_rotationInput, _moveInput);
            else
                _model.MoveVariant(_rotationInput, _moveInput);
        }
    #endregion

    #region Abilities
        #region Hook
        
            if (Input.GetKeyDown(KeyCode.Space)) { _model.Hook(); }
            if(_model.objectToHookUpdated) { _model.lineCurrent?.Invoke(); _model.limitVelocity?.Invoke(); }
            if (Input.GetKeyUp(KeyCode.Space)){ _model.reset?.Invoke(); }
            
        #endregion
    #endregion
        
    }
}
