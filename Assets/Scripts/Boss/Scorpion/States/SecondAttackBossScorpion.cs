using UnityEngine;
using static Utils;

public class SecondAttackBossScorpion : State
{
    private Scorpion _scorpion;

    public SecondAttackBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
        _scorpion._anim.SetBool(SECOND_ATTACK_ANIM_SCORPION, true);
        _scorpion.SecondAttack();
        Debug.Log("ENTER SECOND");

    }

    public override void OnUpdate()
    {
        if (_scorpion._isDead)
            _scorpion.stateMachine.ChangeState(BossScorpionState.DeathScorpion);
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _scorpion._anim.SetBool(SECOND_ATTACK_ANIM_SCORPION, false);
    }
}