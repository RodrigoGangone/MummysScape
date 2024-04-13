using System.Collections;
using System.Collections.Generic;
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
        _model.MoveVariant(_rotationInput, _moveInput);
    }
}
