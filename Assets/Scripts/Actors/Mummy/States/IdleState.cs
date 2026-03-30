using static Animations.Player;

/// <summary> 
/// Estado de Reposo: Punto de entrada neutro de la FSM donde el personaje permanece estático, 
/// orientándose automáticamente hacia el jugador o manteniendo la pose base. 
/// </summary>

public sealed class IdleState : State
{
    private readonly PlayerContext _ctx;
    public IdleState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool(IDLE, true);
        
        _ctx.View?.SetMoveSpeedVisual(0f);
    }

    public override void OnExit()
    {
        _ctx.View.Animator.SetBool(IDLE, false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }
}