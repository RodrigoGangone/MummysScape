using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Hook : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Hook(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }
    
    public override void OnEnter()
    {
        Debug.Log("STATE: HOOK");
        _model.Hook();
    }
    
    public override void OnExit()
    {
        Debug.Log("STATE: HOOK - ON EXIT");
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log("STATE: HOOK - UP SPACE");
            _model.resetSpringForHook?.Invoke();
            OnExit();
        }
        else
        {
            _model.drawBandageHook?.Invoke(); //TODO: Es visual, pasar al view #Maxy
        }
    }

    public override void OnFixedUpdate()
    {
        _model.limitVelocityHook?.Invoke();
    }
}
