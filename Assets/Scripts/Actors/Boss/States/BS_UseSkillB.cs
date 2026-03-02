using static Utils;

/// <summary>
/// Estado de Habilidad Secundaria: Controla la activación y el ciclo de animación del segundo 
/// slot de ataque configurado para el Boss.
/// </summary>

public sealed class BS_UseSkillB : State
{
    private readonly BossActor _actor;
    public BS_UseSkillB(BossActor actor) { _actor = actor; }

    public override void OnEnter()
    {
        _actor.NotifySkillStarted();
        
        _actor.Animator.SetBool(SECONDARY_ANIM_SCORPION, true);
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() => _actor.Animator.SetBool(SECONDARY_ANIM_SCORPION, false);
}