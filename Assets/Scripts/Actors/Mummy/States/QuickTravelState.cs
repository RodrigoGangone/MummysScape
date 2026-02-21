using static PlayerEnum;
using UnityEngine;

/// <summary> 
/// Estado de Viaje Rápido: Conecta al personaje con el sistema de teletransporte (Hippos), cediendo 
/// el control de la posición a la secuencia de transporte hasta que el viaje finalice. 
/// </summary>

public class QuickTravelState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;
    private HippoTravel _hippoTravel;

    public QuickTravelState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        if (!_ctx.TryGetQuickTravel(_ctx.Tf, out _hippoTravel))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        _hippoTravel.BeginTravel(_ctx.Tf);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        if (!_hippoTravel.Link.IsBusy)
            StateMachine.ChangeState(PlayerStateId.Idle);
    }

    public override void OnExit()
    {
    }
}