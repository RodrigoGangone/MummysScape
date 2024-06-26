using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Shoot : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Shoot(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;

        _model.sizeModify += GoIdle;
    }

    public override void OnEnter()
    {
        _view.PLAY_ANIM("Shoot", true);
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Shoot", false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public void GoIdle()
    {
        StateMachine.ChangeState(PlayerState.Idle);
    }
}