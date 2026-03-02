using static Utils;

/// <summary>
/// Estado de Espera: Mantiene al Boss en su pose base mientras rota continuamente para 
/// encarar la posición actual del jugador.
/// </summary>

public sealed class BS_Idle : State
{
    private readonly BossActor _actor;
    public BS_Idle(BossActor actor) { _actor = actor; }
    public override void OnEnter() => _actor.Animator?.SetBool(IDLE_ANIM_SCORPION, true);
    public override void OnFixedUpdate() { }
    public override void OnExit()  => _actor.Animator?.SetBool(IDLE_ANIM_SCORPION, false);
    public override void OnUpdate()
    {
        var t = _actor.Transform;
        var p = _actor.Player.Tf.position;
        p.y = t.position.y;
        t.LookAt(p);
    }
}