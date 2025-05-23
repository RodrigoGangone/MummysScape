using UnityEngine;
using static Utils;

public class FirstAttackBossScorpion : State
{
    private Scorpion _scorpion;

    public FirstAttackBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
        _scorpion._anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, true);
        Debug.Log("ENTER FIRST");

        _scorpion.FirstAreaAttack(_scorpion.player.WalkingSand
            ? _scorpion.pointAttackSand
            : _scorpion.pointAttackPlatform);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _scorpion._anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, false);
    }
}