using UnityEngine;
using static Animations.Boss;

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

        // NUEVO: Validación en el contexto de la habilidad
        // Si el boss es bloqueado por una cinemática en pleno ataque, 
        // abortamos la habilidad localmente para no esperar un Animation Event que nunca llegará.
        if (_actor.IsLocked)
        {
            CancelSkillContext();
            return;
        }

        var t = _actor.Transform;
        var p = _actor.Player.Tf.position;
        p.y = t.position.y;
        t.LookAt(p);
    }

    public override void OnFixedUpdate() { }

    public override void OnExit()
    {
        // Aprovechamos el método centralizado para limpiar si la habilidad termina de forma natural
        CancelSkillContext();
    }

    private void CancelSkillContext()
    {
        // 1. Apagamos la bandera para que el GOAP sepa que ya no estamos atacando
        _actor.NotifySkillEnded(); 
        
        // 2. Limpiamos el Animator localmente
        _actor.Animator.SetBool(PRIMARY_ANIM_SCORPION, false);

        // 3. Destruimos cualquier proyectil que se haya quedado a medio cargar
        var chargingProjectile = _actor.Transform.GetComponentInChildren<ChargeableProjectile>();
        if (chargingProjectile != null)
            Object.Destroy(chargingProjectile.gameObject);
    }
}