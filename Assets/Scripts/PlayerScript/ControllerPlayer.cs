using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerPlayer 
{
    ModelPlayer _model;
    float _rotationInput;
    float _moveInput;
    
    private Transform camaraTransform;
    
    public ControllerPlayer(ModelPlayer m)
    {
        _model = m;
        if (Camera.main != null) camaraTransform = Camera.main.transform;
    }

    public void ControllerUpdate()
    {
        _rotationInput = Input.GetAxis("Horizontal");
        _moveInput = Input.GetAxis("Vertical");
        _model.MoveCameraTransform(_rotationInput, _moveInput, camaraTransform);
    }
}
