using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Dead : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Dead(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        _view.PLAY_ANIM_TRIGGER("Death");
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