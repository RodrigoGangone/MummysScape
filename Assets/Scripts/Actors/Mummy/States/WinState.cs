using UnityEngine;
using static Animations.Player;

public class WinState : State, IBandageRestrictor
{
    private PlayerContext _ctx;
    public WinState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.Animator.SetTrigger(WIN);
        _ctx.View.Shadow.fadeFactor = 0;
        
        Debug.Log("////////////WINSTATE//////////");
        
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Win", true);
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