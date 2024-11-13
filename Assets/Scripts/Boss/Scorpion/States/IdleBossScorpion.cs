using UnityEngine;
using static Utils;

public class IdleBossScorpion : State
{
    private Scorpion _scorpion;

    private float _timeToFirstAttack;
    private float _timeToSecondAttack;

    public IdleBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
        _scorpion._anim.SetBool(IDLE_ANIM_SCORPION, true);
    }

    public override void OnUpdate()
    {
        Vector3 targetPosition = new Vector3(_scorpion.transform.position.x, _scorpion._targetPlayer.position.y,
            _scorpion.transform.position.z);

        _scorpion.transform.LookAt(targetPosition);

        if (_timeToFirstAttack < _scorpion._cdAttack1)
            _timeToFirstAttack += Time.deltaTime;

        if (_timeToSecondAttack < _scorpion._cdAttack2)
            _timeToSecondAttack += Time.deltaTime;

        if (_timeToFirstAttack >= _scorpion._cdAttack1 && _scorpion.currentAttack == 1)
            _scorpion.stateMachine.ChangeState(BossScorpionState.FirstAttackScorpion);

        if (_timeToSecondAttack >= _scorpion._cdAttack2 && _scorpion.currentAttack == 2)
            _scorpion.stateMachine.ChangeState(BossScorpionState.SecondAttackScorpion);

        if (_scorpion._isDead)
            _scorpion.stateMachine.ChangeState(BossScorpionState.DeathScorpion);
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _scorpion._anim.SetBool(IDLE_ANIM_SCORPION, false);

        _timeToFirstAttack = 0;
        _timeToSecondAttack = 0;
    }
}