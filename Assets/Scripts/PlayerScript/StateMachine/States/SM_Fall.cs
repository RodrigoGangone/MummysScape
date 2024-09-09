using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Fall : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;
    
    private float _speed;
    private float _speedRotation;

    public SM_Fall(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;
        
        _speed = _player.Speed;
        _speedRotation = _player.SpeedRotation;
    }

    public override void OnEnter()
    {
        _view.PLAY_ANIM("Fall", true);
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Fall", false);
    }

    public override void OnUpdate()
    {
        if (_model.CheckGround())
            StateMachine.ChangeState(PlayerState.Idle);
    }

    public override void OnFixedUpdate()
    {
        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            _speed,
            _speedRotation);
    }
}