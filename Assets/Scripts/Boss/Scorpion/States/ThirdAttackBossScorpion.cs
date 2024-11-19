using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using static Utils;
using UnityEngine;

public class ThirdAttackBossScorpion : State
{
    private Scorpion _scorpion;
    private Vector3 _initialPosPlayer;
    internal const int SPEED_PROJECTILE = 10;
    private float _lifeTimeStone;
    private const int MAX_LIFETIME_STONE = 5;

    public ThirdAttackBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
        _initialPosPlayer = _scorpion.player.transform.position;

        _scorpion._anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, true);

        _scorpion._stoneView.SetActive(true);
    }

    public override void OnUpdate()
    {
        MoveStone();

        #region Desactive for time

        _lifeTimeStone += Time.deltaTime;

        if (_lifeTimeStone > MAX_LIFETIME_STONE || !_scorpion._stoneView.activeInHierarchy)
            _scorpion.stateMachine.ChangeState(BossScorpionState.IdleScorpion);

        #endregion
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _scorpion._anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, false);

        _scorpion._stoneView.SetActive(false);
        _lifeTimeStone = 0;
    }

    void MoveStone()
    {
        Vector3 dir = (_initialPosPlayer - _scorpion._stonePrefab.transform.position).normalized;

        _scorpion._stonePrefab.transform.position += dir * (SPEED_PROJECTILE * Time.deltaTime);
    }
}