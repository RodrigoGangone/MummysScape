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
        Debug.Log("------ AttractState ------");
        
        // Validación por tamaño
        if (!SizeRules.Can(_ctx.Model.Size, PlayerActionId.Attract))
        { StateMachine.ChangeState(PlayerStateId.Idle); return; }

        // Ray/esfera al frente
        if (!_interactions.TryFindAttractable(_ctx.Tf, out _target, out var hit))
        { StateMachine.ChangeState(PlayerStateId.Idle); return; }

        _targetTf = (hit.collider != null) ? hit.collider.attachedRigidbody?.transform ?? hit.collider.transform : null;
        _time = 0f;
    }

    public override void OnUpdate()
    {
        _time += Time.deltaTime;
        if (_time > _maxTime) { StateMachine.ChangeState(PlayerStateId.Idle); }
    }

    public override void OnFixedUpdate()
    {
        if (_target == null) { StateMachine.ChangeState(PlayerStateId.Idle); return; }

        // Punto objetivo = un poco delante del player (evita chocar)
        Vector3 aim = _ctx.Rb.position + _ctx.Tf.forward * _interactions.StopDistance;

        // Aplica atracción
        bool pulled = _target.PullTowards(aim, _interactions.PullStrength, _interactions.PullMaxSpeed);

        // Si ya está cerca, terminar
        if (_targetTf != null)
        {
            Vector3 flat = _targetTf.position; flat.y = _ctx.Rb.position.y;
            if ((flat - aim).sqrMagnitude <= (_interactions.StopDistance * _interactions.StopDistance))
            {
                StateMachine.ChangeState(PlayerStateId.Idle);
                return;
            }
        }

        if (!pulled) StateMachine.ChangeState(PlayerStateId.Idle);
    }

    public override void OnExit() { _target = null; _targetTf = null; }
}
