using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Walk : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Walk(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        Debug.Log("STATE: WALK");
    }

    public override void OnExit()
    {
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        _model.MoveVariant(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
    }
}