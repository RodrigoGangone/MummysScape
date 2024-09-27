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
        _player._viewPlayer.PLAY_ANIM("Shoot", true);
        _player._modelPlayer.RotatePreShoot();
    }

    public override void OnExit()
    {
        _player._viewPlayer.PLAY_ANIM("Shoot", false);
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