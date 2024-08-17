using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SM_Shoot : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;
    private Vector3? targetButtonPosition;
    
    public SM_Shoot(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;

        _model.sizeModify += GoIdle;
    }

    public override void OnEnter()
    {
        _view.PLAY_ANIM("Shoot", true);
        //_model.RotatePreShoot();
        targetButtonPosition = _model.CheckForButtonNEW(); //obtengo la posicion del boton o null
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Shoot", false);
    }

    public override void OnUpdate()
    {
        if (targetButtonPosition.HasValue)
        {
            _model.RotatePreShootNew(targetButtonPosition.Value);
        }
    }

    public override void OnFixedUpdate()
    {
    }

    public void GoIdle()
    {
        StateMachine.ChangeState(PlayerState.Idle);
    }
}