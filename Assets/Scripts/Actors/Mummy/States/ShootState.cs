using static Animations.Player;
using static SfxIDs;

/// <summary> 
/// Estado de Disparo: Activa la animación y los efectos sonoros de lanzamiento de vendas, sirviendo 
/// como el disparador visual para la creación de proyectiles físicos. 
/// </summary>

public sealed class ShootState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;

    public ShootState(PlayerContext ctx)
    {
        _ctx = ctx;
    }

    public override void OnEnter()
    {
        _ctx.View.PlaySfx(Mummy___Normal.Shoot);
        
        _ctx.View.Animator.SetTrigger(SHOOT);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }
}
