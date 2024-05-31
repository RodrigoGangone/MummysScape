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
    }
    public override void OnEnter()
    {
        Debug.Log("STATE: SHOOT");
        _view.PLAY_ANIM("Shoot");
    }

    public override void OnExit()
    {
    }
    
    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }
}
