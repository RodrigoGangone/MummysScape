using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class SecondAttackBossScorpion : State
{
    private SecondAttackProperties _secondAttackProperties;

    private int _completedGeysersCount; // Contador de géiseres completados
    private List<Geyser> _currentStageGeysers; // Géiseres activos según la etapa actual

    public SecondAttackBossScorpion(SecondAttackProperties secondAttackProperties)
    {
        _secondAttackProperties = secondAttackProperties;
    }

    public static void SetAnimator(Animator animator)
    {
    }

    public static void SetPositions(Transform ScorpionPos, Transform PlayerPos)
    {
    }

    public override void OnEnter()
    {
        //  _scorpion._anim.SetBool(SECOND_ATTACK_ANIM_SCORPION, true);
//
        //  // Seleccionar los géiseres correspondientes a la etapa actual
        //  _currentStageGeysers = _scorpion.GetCurrentStageGeysers();
//
        //  if (_currentStageGeysers == null || _currentStageGeysers.Count == 0)
        //  {
        //      Debug.LogWarning("No hay géiseres definidos para la etapa actual.");
        //      _scorpion.stateMachine.ChangeState(BossScorpionState.IdleScorpion);
        //      return;
        //  }
//
        //  // Iniciar el ataque con los géiseres y pasar el callback para contar los completados
        //  _scorpion.SecondAttack(OnGeyserCompleted);
//
        //  Debug.Log("ENTER SECOND");
    }

    public override void OnUpdate()
    {
        // Si todos los géiseres han terminado, cambiamos al estado Idle
        if (_completedGeysersCount == _currentStageGeysers.Count)
        {
            //_scorpion.stateMachine.ChangeState(BossScorpionState.IdleScorpion);
        }
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        //   Debug.Log("EXIT SECOND");
        //   _scorpion._anim.SetBool(SECOND_ATTACK_ANIM_SCORPION, false);
        //   _completedGeysersCount = 0;
    }

    // Este método será llamado cuando un géiser termine su secuencia
    private void OnGeyserCompleted()
    {
        _completedGeysersCount++;
        Debug.Log($"Géiseres completados: {_completedGeysersCount}/{_currentStageGeysers.Count}");
    }
}