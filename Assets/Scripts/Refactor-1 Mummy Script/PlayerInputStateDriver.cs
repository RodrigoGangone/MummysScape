
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

    //TODO: ver que hacer con los "if" previos a pasar de state,ya que provocan entrar al state 1 vez por frame.
    private void Update()
    {
        if (_ctx == null || _sm == null || _input == null) return;

        var mv = _input.Move;

        // 1) Ambiente: caída 
        if (!_ctx.IsGrounded())
        {
            _sm.ChangeState(Fall);
            return;
        }

        // 2) Space press => Smash (Head). Si el guard/SizeRules no dejan, sigue el flujo.
        if (_input.ConsumeSpaceDown())
        {
            if (_sm.ChangeState(Smash)) return;
        }

        // 3) Space hold => Swing > Attract (según target frente)
        if (_input.IsSpaceHeld())
        {
            if (_ctx.TryGetSwingTarget(out _)) // Small: el guard lo permite; otros tamaños lo bloquean
            {
                _sm.ChangeState(Swing); 
                return;
            }

            if (_ctx.TryGetAttractTarget(out _)) // Normal: permitido; otros tamaños bloquean
            {
                _sm.ChangeState(Attract);
                return;
            }
        }

        // 4) Edge: E / Q
        if (_input.ConsumeShootDown())
        {
            if (_sm.ChangeState(Shoot)) return;
        }

        if (_input.ConsumeDropDown())
        {
            if (_sm.ChangeState(DropBandage)) return;
        }

        // 5) Movimiento base + 
        if (Mathf.Abs(mv.x) > _moveDeadZone || Mathf.Abs(mv.y) > _moveDeadZone)
        {
            if (_ctx.TryGetPushTarget(out _ , out _))
            {
                _sm.ChangeState(Push); 
                return;
            }
            _sm.ChangeState(Walk);
        }
        else
            _sm.ChangeState(Idle);
    }
}
