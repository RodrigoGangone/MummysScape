using UnityEngine;
using static PlayerEnum.PlayerStateId;

/// <summary>
/// PlayerInputStateDriver
/// Orquesta la intención de entrada a estados a partir del input del jugador.
/// Delegamos la validación de entrada a Push en PlayerTransitionGuard.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(StateMachinePlayer))]
public class PlayerInputStateDriver : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField, Min(0f)] private float _moveDeadZone = 0.1f;

    [Header("Push Gating")]
    [SerializeField, Range(0f,1f)] private float _enterPushAxisDot = 0.6f;
    [SerializeField, Range(0f,90f)] private float _enterPushAngleDeg = 35f;

    private StateMachinePlayer _sm;
    private PlayerContext _ctx;
    private IPlayerInput _input;
    private PlayerTransitionGuard _guard;

    public void Bind(PlayerContext ctx, StateMachinePlayer sm, PlayerTransitionGuard guard = null)
    {
        _ctx = ctx;
        _sm = sm;
        _input = ctx.Input;
        _guard = guard ?? new PlayerTransitionGuard(ctx); // simple y explícito
    }

    private void Awake()
    {
        if (_sm == null) _sm = GetComponent<StateMachinePlayer>();
    }

    private void Update()
    {
        if (_ctx == null || _sm == null || _input == null) return;

        var mv = _input.Move;

        // 1) Caída
        if (!_ctx.IsGrounded())
        {
            _sm.ChangeState(Fall);
            return;
        }

        // 2) Acciones instantáneas
        if (_input.ConsumeSpaceDown()) { if (_sm.ChangeState(Smash)) return; }
        if (_input.ConsumeShootDown()) { if (_sm.ChangeState(Shoot)) return; }
        if (_input.ConsumeDropDown())  { if (_sm.ChangeState(DropBandage)) return; }

        bool wantsMove = Mathf.Abs(mv.x) > _moveDeadZone || Mathf.Abs(mv.y) > _moveDeadZone;

        // 4) Push con gating delegado al Guard
        if (_guard.CanEnterPush(_moveDeadZone, _enterPushAxisDot, _enterPushAngleDeg))
        {
            var current = _sm.CurrentId();
            if (current is PlayerEnum.PlayerStateId id && id == Push) return; // evitar fallback
            if (_sm.ChangeState(Push)) return; // si entré, corto
        }

        // 5) Walk / Idle (fallback)
        _sm.ChangeState(wantsMove ? Walk : Idle);
    }
}