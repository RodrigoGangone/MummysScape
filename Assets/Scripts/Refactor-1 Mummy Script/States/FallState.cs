
using UnityEngine;

public class FallState : State
{
    private readonly PlayerContext _ctx;
    public FallState(PlayerContext ctx) => _ctx = ctx;
    
    public override void OnEnter() { Debug.Log("FallState!"); }
    public override void OnUpdate() { }
    public override void OnFixedUpdate() { }
    public override void OnExit() { }
}
