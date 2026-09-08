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
        
        // Nos suscribimos al evento para escuchar bloqueos
        _actor.OnLockStateChanged += HandleLock;
    }

    private void HandleLock(bool isLocked)
    {
        if (isLocked) 
        {
            CleanUpLocal();
            // Acá aplicamos tu idea: Mandamos directo a Idle al interrumpir
            _actor.AbortCurrentSkill(); 
        }
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
        _actor.OnLockStateChanged -= HandleLock;
        
        // Aseguramos la limpieza del Animator tanto si salimos por un Lock 
        // como si la habilidad terminó de manera normal y natural.
        CleanUpLocal();
    }

    private void CleanUpLocal()
    {
        _actor.Animator.SetBool(PRIMARY_ANIM_SCORPION, false);

        var chargingProjectile = _actor.Transform.GetComponentInChildren<ChargeableProjectile>();
        if (chargingProjectile != null)
            Object.Destroy(chargingProjectile.gameObject);
    }
}