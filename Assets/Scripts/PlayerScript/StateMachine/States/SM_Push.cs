using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Push : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Push(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;
    }

    public override void OnEnter()
    {
        //TODO: Agregar animacion de empujar
        //_view.PLAY_ANIM("PrepareToPush", true);
        Debug.Log("PrepareToPush");
    }

    public override void OnExit()
    {
        //_view.PLAY_ANIM("PrepareToPush", false);
        Debug.Log("NO Push");
    }

    public override void OnUpdate()
    {
        //_view.PLAY_ANIM("Push", IsPushing());
    }

    public override void OnFixedUpdate()
    {
        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
    }

    private bool IsPushing()
    {
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }
}