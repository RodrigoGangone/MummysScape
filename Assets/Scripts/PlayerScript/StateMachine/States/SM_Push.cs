using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Push : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;

    private float _speed;
    private float _speedRotation;

    public SM_Push(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;

        _speed = _player.Speed * 0.5f;
        _speedRotation = _player.SpeedRotation * 0.5f;
    }

    public override void OnEnter()
    {
        //_view.PLAY_ANIM("PrepareToPush", true);
        Debug.Log("OnEnter: SM_PUSH");
    }

    public override void OnExit()
    {
        //_view.PLAY_ANIM("PrepareToPush", false);
        Debug.Log("OnExit: SM_PUSH");
    }

    public override void OnUpdate()
    {
        if (_model.CurrentBox == null || !_model.CurrentBox.GetComponent<PushPullObject>().BoxInFloor()) 
            StateMachine.ChangeState(PlayerState.Idle);
        
        // Mov de la box en update xq es por transform
        if (_model.CurrentBox != null && _model.CurrentBox.GetComponent<PushPullObject>().BoxInFloor())
            _model.CurrentBox.transform.position += _model.DirToPush * (_player.SpeedPush * Time.deltaTime);
    }

    public override void OnFixedUpdate()
    {
        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            _speed,
            _speedRotation);
    }
}