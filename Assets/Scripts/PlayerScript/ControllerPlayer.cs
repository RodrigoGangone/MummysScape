using System;
using UnityEngine;

public class ControllerPlayer 
{
    ModelPlayer _model;
    float _rotationInput;
    float _moveInput;
    
    public ControllerPlayer(ModelPlayer m)
    {
        _model = m;
    }

    public void ControllerUpdate()
    {
        _rotationInput = Input.GetAxis("Horizontal");
        _moveInput = Input.GetAxis("Vertical");
        
        if (_rotationInput != 0 || _moveInput != 0) { _model.MoveVariant(_rotationInput, _moveInput); }

        if (Input.GetKeyDown(KeyCode.Space)) { _model.Hook(); }
        if (_model._drawLineHook){ _model.DrawHook(); }
        if(_model._isHook && !_model._drawLineHook ){_model._lineCurrent?.Invoke();}
        if (Input.GetKeyUp(KeyCode.Space)){_model._reset?.Invoke();}
    }
}
