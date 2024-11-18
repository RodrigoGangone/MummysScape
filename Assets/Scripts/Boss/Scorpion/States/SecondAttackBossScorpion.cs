using System.Linq;
using UnityEngine;
using static Utils;

public class SecondAttackBossScorpion : State
{
    private Scorpion _scorpion;
    private int _completedGeysersCount; // Contador de géiseres completados

    public SecondAttackBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
        _scorpion._anim.SetBool(SECOND_ATTACK_ANIM_SCORPION, true);

        // Iniciar el ataque con los géiseres y pasar el callback para contar los completados
        _scorpion.SecondAttack(OnGeyserCompleted);

        Debug.Log("ENTER SECOND");
    }

    public override void OnUpdate()
    {
        // Si todos los géiseres han terminado, cambiamos al estado Idle
        if (_completedGeysersCount == _scorpion._geysers.Count)
        {
            _scorpion.stateMachine.ChangeState(BossScorpionState.IdleScorpion);
        }

        if (_scorpion._isDead)
            _scorpion.stateMachine.ChangeState(BossScorpionState.DeathScorpion);
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        Debug.Log("EXIT SECOND");
        _scorpion._anim.SetBool(SECOND_ATTACK_ANIM_SCORPION, false);
        _completedGeysersCount = 0;
    }

    // Este método será llamado cuando un géiser termine su secuencia
    private void OnGeyserCompleted()
    {
        _completedGeysersCount++;

        Debug.Log($"Géiseres completados: {_completedGeysersCount}/{_scorpion._geysers.Count}");
    }
}