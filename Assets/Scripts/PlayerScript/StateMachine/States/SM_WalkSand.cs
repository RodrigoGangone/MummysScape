using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_WalkSand : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_WalkSand(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;
    }

    public override void OnEnter()
    {
        Debug.Log("ON ENTER STATE WALKSAND");
        _view.PLAY_ANIM("WalkSand", true);
        _view.PLAY_WALK_SAND(true);
    }

    public override void OnExit()
    {
        _model.ClampMovement();
        _view.PLAY_WALK_SAND(false);
        _view.PLAY_ANIM("WalkSand", false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            _player.Speed,
            _player.SpeedRotation);
    }
}