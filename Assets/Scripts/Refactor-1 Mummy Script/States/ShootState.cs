using UnityEngine;
using static PlayerEnum;

/// <summary>
/// ShootState
/// Ejecuta el disparo (placeholder) y vuelve a Idle. Valida SizeRules.
/// </summary>
public sealed class ShootState : State
{
    private readonly PlayerContext _ctx;
    public ShootState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("Shoot!");
        
        // Si no hay vendas vuelvo a idle, sino las consumo
        //if (!_ctx.Model.TryConsumeBandage())
        //{
        //    Debug.Log("Shoot! PERO SIN VENDAS");
        //}
        
        /*
        // TODO: tu raycast/proyectil real
        _ctx.View?.PlayShoot();
        */
    }

    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}