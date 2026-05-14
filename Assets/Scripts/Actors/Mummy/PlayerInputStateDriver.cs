using UnityEngine;
using static PlayerEnum.PlayerStateId;

[DisallowMultipleComponent]
[RequireComponent(typeof(StateMachinePlayer))]
public class PlayerInputStateDriver : MonoBehaviour, IPausable, ILocked
{
    [Header("Tuning")] [SerializeField, Min(0f)]
    private float _moveDeadZone = 0.1f;

    private StateMachinePlayer _sm;
    private PlayerContext _ctx;
    private IPlayerInput _input;
    private bool _paused, _locked;

    public void Bind(PlayerContext ctx, StateMachinePlayer sm)
    {
        _ctx = ctx;
        _sm = sm;
        _input = ctx.Input;
        _sm.SetRedirector(new PlayerSizeRedirector(ctx));
    }

    private void Awake()
    {
        if (_sm == null) _sm = GetComponent<StateMachinePlayer>();
    }

    private void Update()
    {
        if (_ctx == null || _sm == null || _input == null || _paused || _locked) return;

        if (_ctx.HasExternalImpact)
        {
            if (!_sm.IsCurrent(KnockBack))
                _sm.ChangeState(KnockBack);
            return;
        }
        
        if (_sm.IsCurrent(Fake))
            return;

        var mv = _input.Move;
        if (_ctx.ShouldForceIdle())
        {
            _sm.ChangeState(_ctx.IsGrounded() ? Idle : Fall);
            return;
        }

        // --- NUEVA CONSULTA DE TRANSICIONES FAKE ---
        if (CheckFakeTransitions(mv)) return;

        bool moving = IsMoving(mv);

        // --- LÓGICA AIRE ---
        if (!_ctx.IsGrounded())
        {
            if (_sm.IsCurrent(Swing))
            {
                if (!_input.IsSpaceHeld()) _sm.ChangeState(Fall);
                return;
            }

            if (CanEnter(Swing) && _input.IsSpaceHeld() && _ctx.TryGetSwingTarget(out _))
            {
                _sm.ChangeState(Swing);
                return;
            }

            if (!_sm.IsCurrent(Fall)) _sm.ChangeState(Fall);
            return;
        }

        // --- LÓGICA SUELO (ACCIONES) ---
        if (_input.IsSpaceHeld())
        {
            if (_sm.IsCurrent(Swing) || _sm.IsCurrent(Attract)) return;

            if (CanEnter(Swing) && _ctx.TryGetSwingTarget(out _))
            {
                _sm.ChangeState(Swing);
                return;
            }

            if (CanEnter(Attract) && _ctx.TryGetAttractTarget(out _))
            {
                _sm.ChangeState(Attract);
                return;
            }
        }
        
        if (_input.ConsumeSpaceDown())
        {
            if (CanEnter(QuickTravel) && _ctx.TryGetQuickTravel(_ctx.Tf, out _))
            {
                _sm.ChangeState(QuickTravel);
                return;
            }

            if (CanEnter(Smash) && _sm.ChangeState(Smash)) return;
        }

        if (_input.IsAimHeld() && CanEnter(Shoot))
        {
            if (_input.ConsumeShootDown())
            {
                if (_ctx.IsAimValid) _sm.ChangeState(Shoot);
                else _sm.ChangeState(Aim);
                return;
            }

            if (!_sm.IsCurrent(Shoot)) _sm.ChangeState(Aim);
            return;
        }

        if (_input.ConsumeDropDown() && CanEnter(DropBandage))
        {
            if (_sm.ChangeState(DropBandage)) return;
        }

        if (moving && CanEnter(Push) && _ctx.TryGetPushTarget(out _, out _, out _))
        {
            if (_sm.IsCurrent(Push)) return;
            if (_sm.ChangeState(Push)) return;
        }

        if (moving) _sm.ChangeState(Walk);
        else _sm.ChangeState(Idle);
    }

    // --- MÉTODO AUXILIAR PARA CONTROLAR INTENTOS FALLIDOS (FAKE) ---
    private bool CheckFakeTransitions(Vector2 move)
    {
        var size = _ctx.Model.Size;
        bool isGrounded = _ctx.IsGrounded();

        // 1. SWING (Aire y Suelo)
        if (CanStartFake(Swing, size) && _input.IsSpaceHeld() && _ctx.TryGetSwingTarget(out _))
        {
            return TryStartFake(Swing);
        }

        if (isGrounded)
        {
            // 2. ATTRACT
            if (CanStartFake(Attract, size) && _input.IsSpaceHeld() && _ctx.TryGetAttractTarget(out _))
            {
                return TryStartFake(Attract);
            }

            // 7. PUSH (Requiere intención de movimiento y objetivo detectado)
            bool moving = IsMoving(move);
            if (moving && CanStartFake(Push, size) && _ctx.TryGetPushTarget(out _, out _, out _))
            {
                return TryStartFake(Push);
            }
        }

        return false;
    }

    private bool TryStartFake(PlayerEnum.PlayerStateId attemptedState)
    {
        _ctx.AttemptedState = attemptedState;
        return _sm.ChangeState(Fake);
    }

    private bool CanStartFake(PlayerEnum.PlayerStateId attemptedState, PlayerEnum.PlayerSize size)
    {
        return _ctx.Model.CanUseAbility(attemptedState)
               && !SizeRules.Can(size, attemptedState)
               && _ctx.Feedback != null
               && _ctx.Feedback.HasFeedback(attemptedState, size);
    }

    private bool CanEnter(PlayerEnum.PlayerStateId state) => _ctx.Model.CanUseAbility(state);
    private bool IsMoving(Vector2 move) => Mathf.Abs(move.x) > _moveDeadZone || Mathf.Abs(move.y) > _moveDeadZone;

    public void OnPauseChanged(bool paused) => _paused = paused;

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);
    }

    public void OnLockChanged(bool isLocked) => _locked = isLocked;
}
