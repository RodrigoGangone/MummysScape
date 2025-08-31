
using UnityEngine;
using static PlayerEnum.PlayerStateId;
using static PlayerEnum.PlayerSize;

/// <summary>
/// PlayerInputStateDriver
/// Traduce inputs crudos a Estados destino y ejecuta StateMachine.ChangeState.
/// Priorización:
/// 1) Ambiente: Fall si no está en suelo.
/// 2) Space (hold): Head->Smash; si no, Attract si hay target al frente.
/// 3) Edge: E->Shoot; Q->DropBandage.
/// 4) Movimiento: Walk/Idle según deadzone.
/// Todas las transiciones pasan por el Guard (TransitionRules + SizeRules).
/// </summary>

[DisallowMultipleComponent]
[RequireComponent(typeof(StateMachinePlayer))]
public class PlayerInputStateDriver : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField, Min(0f)] private float _moveDeadZone = 0.1f;

    private StateMachinePlayer _sm;
    private PlayerContext _ctx;
    private IPlayerInput _input;
    

    public void Bind(PlayerContext ctx, StateMachinePlayer sm)
    {
        _ctx = ctx;
        _sm = sm;
        _input = ctx.Input;
    }

    private void Awake()
    {
        if (_sm == null) _sm = GetComponent<StateMachinePlayer>();
    }

    private void Update()
    {
        if (_ctx == null || _sm == null || _input == null) return;

        var size = _ctx.Model.Size;
        var grounded = _ctx.IsGrounded();
        var mv = _input.Move;

        // 1) Ambiente: caída
        if (!grounded)
        {
            _sm.ChangeState(Fall);
            return;
        }

        // 2) Space (hold): Smash (Head) > Attract (si hay target)
        if (_input.IsSpaceHeld())
        {
            if (size == Head)
            {
                _sm.ChangeState(Smash);
                return;
            }

            if (size == Normal)
            {
                if (_ctx.TryGetAttractTarget(out _))
                {
                    _sm.ChangeState(Attract);
                    return;
                }
            }
        }

        // 3) Edge: E / Q
        if (_input.ConsumeShootDown())
        {
            _sm.ChangeState(Shoot);
            return;
        }

        if (_input.ConsumeDropDown())
        {
            _sm.ChangeState(DropBandage);
            return;
        }

        // 4) Movimiento base
        if (Mathf.Abs(mv.x) > _moveDeadZone || Mathf.Abs(mv.y) > _moveDeadZone)
            _sm.ChangeState(Walk);
        else
            _sm.ChangeState(Idle);
    }
}
