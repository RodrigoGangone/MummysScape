using UnityEngine;
using static PlayerEnum;
/// <summary>
/// IdleState
/// </summary>
public sealed class IdleState : State
{
    private readonly PlayerContext _ctx;
    public IdleState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View?.SetMoveSpeedVisual(0f);
        Debug.Log("IdleState!");
    }
    public override void OnExit()  { }

    public override void OnUpdate() { }

    public override void OnFixedUpdate() { }
}