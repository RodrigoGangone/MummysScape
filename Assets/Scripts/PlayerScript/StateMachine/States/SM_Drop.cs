using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Drop : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Drop(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        Debug.Log("OnEnter - SM_Drop");
        _model.SpawnBandage();
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        Debug.Log("OnExit - SM_Drop");
    }
}
