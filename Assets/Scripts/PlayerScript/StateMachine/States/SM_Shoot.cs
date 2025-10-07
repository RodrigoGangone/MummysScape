using UnityEngine;

public class SM_Shoot : State
{
    private Player _player;
    private Vector3? targetButtonPosition; //Posicion del boton hacia el que apunta

    public SM_Shoot(Player player)
    {
        _player = player;
        _player.SizeModify += GoIdle;
    }

    public override void OnEnter()
    {
        if (_player.CurrentPlayerSize.Equals(PlayerSize.Normal))
            _player._viewPlayer.PLAY_ANIM("ShootNormal", true);
        else
            _player._viewPlayer.PLAY_ANIM("ShootSmall", true);
            
        _player._modelPlayer.RotatePreShoot();
    }

    public override void OnExit()
    {
        _player._viewPlayer.PLAY_ANIM("ShootNormal", false);
        _player._viewPlayer.PLAY_ANIM("ShootSmall", false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    private void GoIdle()
    {
        StateMachine.ChangeState(PlayerState.Idle);
    }
}