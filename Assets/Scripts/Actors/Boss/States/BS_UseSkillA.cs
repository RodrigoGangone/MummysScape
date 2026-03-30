using UnityEngine;
using static Animations.Boss;

/// <summary>
/// Estado de Habilidad Primaria: Gestiona el ciclo del primer slot de ataque, asegurando que el Boss 
/// mire al jugador y limpiando proyectiles residuales al finalizar la ejecución.
/// </summary>

public sealed class BS_UseSkillA : State
{
    private readonly BossActor _actor;

    public BS_UseSkillA(BossActor actor) => _actor = actor;

    public override void OnEnter()
    {
        _actor.NotifySkillStarted();
        _actor.Animator.SetBool(PRIMARY_ANIM_SCORPION, true);
    }

    public override void OnUpdate()
    {
        if (!_actor.IsExecutingSkill) return;

        var t = _actor.Transform;
        var p = _actor.Player.Tf.position;
        p.y = t.position.y;
        t.LookAt(p);
    }

    public override void OnFixedUpdate() { }

    public override void OnExit()
    {
        _actor.Animator.SetBool(PRIMARY_ANIM_SCORPION, false);

        var chargingProjectile = _actor.Transform.GetComponentInChildren<ChargeableProjectile>();

        if (chargingProjectile != null)
            Object.Destroy(chargingProjectile.gameObject);
    }
}