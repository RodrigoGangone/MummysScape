using static PlayerEnum;
using UnityEngine;

public class QuickTravelState : State
{
    private readonly PlayerContext _ctx;
    private HippoTravel _hippoTravel;

    public QuickTravelState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("QuickTravelState! ------------ Enter");

        if (!_ctx.TryGetQuickTravel(_ctx.Tf, out _hippoTravel))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        _hippoTravel.BeginTravel(_ctx.Tf);

        //APAGAR VIEW DEL PLAYER
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
        Debug.Log("QuickTravelState! ------------ Exit");
        //ENCENDER VIEW DEL PLAYER
    }
}