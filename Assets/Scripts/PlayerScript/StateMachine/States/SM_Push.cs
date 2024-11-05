using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Push : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;

    private float _speed;
    private float _speedRotationOfPlayer;

    private float _speedRotateToDirToPush = 50f;

    private Vector3 velocity = Vector3.zero; // Para SmoothDamp


    public SM_Push(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;

        _speed = _player.Speed * 0.25f;
        _speedRotationOfPlayer = 0f;
    }

    public override void OnEnter()
    {
        _view.PLAY_ANIM("Push", true);
        Debug.Log("OnEnter: SM_PUSH");
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Push", false);
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
        if (_model.DirToPush != Vector3.zero)
        {
            Quaternion currentRotation = _player.transform.rotation;
            Vector3 targetDirection = new Vector3(_model.DirToPush.x, 0, _model.DirToPush.z);
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            _player.transform.rotation = Quaternion.RotateTowards(
                currentRotation,
                targetRotation,
                _speedRotateToDirToPush * Time.fixedDeltaTime
            );
        }

        // Muevo MUY suavemente al player al centro del lado que estoy empujando
        if (_model.CurrentBoxSide != null)
        {
            Vector3 centerOfSide = _model.CurrentBoxSide.GetChild(0).position;
            Vector3 playerPosition = _player.transform.position;

            // muevo solo en XZ
            Vector3 targetPosition = new Vector3(centerOfSide.x, playerPosition.y, centerOfSide.z);

            // smoothDamp para un movimiento suave para rigibodys
            _player._rigidbody.MovePosition(Vector3.SmoothDamp(
                playerPosition,
                targetPosition,
                ref velocity,
                0.25f
            ));
        }

        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            _speed,
            _speedRotationOfPlayer);
    }
}