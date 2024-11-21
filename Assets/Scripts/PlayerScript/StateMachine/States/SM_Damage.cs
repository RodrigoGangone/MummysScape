using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Damage : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Damage(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        //PLAY ANIMACION DE DAMAGE
    }

    public override void OnExit()
    {
        //STOP ANIMACION DE DAMAGE
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }
}