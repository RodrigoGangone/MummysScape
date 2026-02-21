using static Utils;

/// <summary>
/// Estado de Muerte: Dispara el trigger de animación final del Boss al agotarse sus etapas 
/// de resistencia.
/// </summary>

public sealed class BS_Die : State
{
    private readonly BossActor _actor;
    public BS_Die(BossActor actor) { _actor = actor; }
    public override void OnEnter() => _actor.Animator?.SetTrigger(DIE_ANIM_SCORPION);
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}