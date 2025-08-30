using UnityEngine;
using static PlayerEnum;
/// <summary>
/// AttractState
/// Busca un IAttractable al frente y lo atrae un corto lapso hasta acercarlo.
/// Prioridad de Space: si estás en Head -> Smash; si no, Attract.
/// </summary>
public sealed class AttractState : State
{
    private readonly PlayerContext _ctx;
    private readonly InteractionRuntime _interactions;
    private IAttractable _target;
    private Transform _targetTf;
    private float _time;
    private const float _maxTime = 1.0f; // seguridad para no quedar colgado

    public AttractState(PlayerContext ctx, InteractionRuntime interactions)
    { _ctx = ctx; _interactions = interactions; }

    public override void OnEnter()
    {
        Debug.Log("AttractState");

        //TODO: esto por el momento esta debug. La idea seria que entre a este STATE luego de hacer match con el objeto y tenga el OKAY para poder pullearlo.
        
        // Ray/esfera al frente
        /*if (!_interactions.TryFindAttractable(_ctx.Tf, out _target, out var hit))
        { StateMachine.ChangeState(PlayerStateId.Idle); return; }*/
        
        StateMachine.ChangeState(PlayerStateId.Idle);
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate() { }

    public override void OnExit() { }
}
