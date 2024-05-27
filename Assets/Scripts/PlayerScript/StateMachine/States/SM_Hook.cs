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
        _model._bandageHook.enabled = true;

        _view.hookMaterial.SetFloat("_leftThreshold", Mathf.Lerp(-1.5f, 1.5f, 2f));
    }

    public override void OnExit()
    {
        Debug.Log("STATE: HOOK - ON EXIT");
        _model.resetSpringForHook?.Invoke();      
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Debug.Log("STATE: HOOK - UP SPACE");
            StateMachine.ChangeState(PlayerState.Fall);
        }
        else
        {
            _model.drawBandageHook?.Invoke(); //TODO: Es visual, pasar al view #Maxy
            IsSwinging();
        }
    }

    public override void OnFixedUpdate()
    {
        _model.limitVelocityRB?.Invoke();

        if (!IsSwinging()) return;
        _model.MoveHooked(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
    }

    private bool IsSwinging()
    {
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }
}