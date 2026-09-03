using static Animations.Boss;

public class BS_Pre_Die : State
{
    private readonly BossActor _actor;
    public BS_Pre_Die(BossActor actor) { _actor = actor; }
    public override void OnEnter() => _actor.Animator?.SetTrigger(PRE_DIE_ANIM_SCORPION);
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}