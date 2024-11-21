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
        _scorpion.viewScorpion.transform.LookAt(_scorpion.player.transform);

        SelectCurrentAttack();

        if (_timeToFirstAttack < _scorpion._cdAttack1)
            _timeToFirstAttack += Time.deltaTime;

        if (_timeToSecondAttack < _scorpion._cdAttack2)
            _timeToSecondAttack += Time.deltaTime;

        if (_timeToFirstAttack >= _scorpion._cdAttack1 && _scorpion._currentAttack == CurrentAttack.First)
            _scorpion.stateMachine.ChangeState(BossScorpionState.ThirdAttackScorpion);

        if (_timeToSecondAttack >= _scorpion._cdAttack2 && _scorpion._currentAttack == CurrentAttack.Second)
            _scorpion.stateMachine.ChangeState(BossScorpionState.SecondAttackScorpion);
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

    private void SelectCurrentAttack()
    {
        var currentState = _scorpion.player._stateMachinePlayer.getCurrentState();

        _scorpion._currentAttack = currentState is STATE_HOOK or STATE_FALL
            ? CurrentAttack.Second
            : CurrentAttack.First;
    }
}