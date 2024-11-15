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
        Debug.Log("ENTER IDLE");
    }

    public override void OnUpdate()
    {
        _scorpion._viewScorpion.transform.LookAt(_scorpion.player.transform);

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

        //Debug.Log("Time to first attack" + _timeToFirstAttack);
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