using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Shoot : State
{
    Player _player;
    public SM_Shoot(Player player)
    {
        _player = player;
    }
    public override void OnEnter()
    {
        _player._modelPlayer.Shoot();
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }
}
