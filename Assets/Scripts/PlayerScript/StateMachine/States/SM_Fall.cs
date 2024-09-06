using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Fall : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Fall(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        Debug.Log("ON ENTER FALL");
        _view.PLAY_ANIM("Fall", true);
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Fall", false);
        Debug.Log("ONEXIT STATE: FALL");
    }

    public override void OnUpdate()
    {
        if (_model.CheckGround())
            StateMachine.ChangeState(PlayerState.Idle);
    }

    public override void OnFixedUpdate()
    {
        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
    }
}