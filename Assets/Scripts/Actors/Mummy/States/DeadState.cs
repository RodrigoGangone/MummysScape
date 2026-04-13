using static Animations.Player;
using static SfxIDs;

/// <summary> 
/// Estado de Muerte: Dispara la secuencia final de derrota, activando las animaciones correspondientes 
/// y bloqueando de forma permanente los controles mediante el sistema de eventos. 
/// </summary>

public class DeadState : State, IBandageRestrictor
{
    private PlayerContext _ctx;
    public DeadState(PlayerContext ctx) => _ctx = ctx;
    
    public override void OnEnter() 
    {
        _ctx.View.PlaySfx(Mummy___Normal.Death);
        
        _ctx.View.Animator.SetTrigger(DEAD);

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Death", true);
    }
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}