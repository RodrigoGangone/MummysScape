using UnityEngine;
using static PlayerEnum.PlayerStateId;

/// <summary> 
/// Driver de Decisiones: Traduce los inputs crudos en solicitudes de cambio de estado para la FSM, 
/// aplicando una jerarquía de prioridades (caída > acciones > movimiento). 
/// </summary>
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

        // Instalamos la lógica de redirección solo para el jugador
        _sm.SetRedirector(new PlayerSizeRedirector(ctx));
    }

    private void Awake()
    {
        if (_sm == null) _sm = GetComponent<StateMachinePlayer>();
    }

    private void Update()
    {
        if (_ctx == null || _sm == null || _input == null || _paused || _locked) return;

        // El Knockback es una reacción externa, no depende de habilidades desbloqueadas
        if (_ctx.HasExternalImpact)
        {
            if (!_sm.IsCurrent(KnockBack))
                _sm.ChangeState(KnockBack);

            return;
        }
        
        if (_sm.IsCurrent(Fake))
            return;

        var mv = _input.Move;
        bool moving = Mathf.Abs(mv.x) > _moveDeadZone || Mathf.Abs(mv.y) > _moveDeadZone;

        // --- LÓGICA AIRE ---
        
        if (!_ctx.IsGrounded())
        {
            if (_sm.IsCurrent(Swing))
            {
                if (!_input.IsSpaceHeld())
                {
                    _sm.ChangeState(Fall);
                }

                return;
            }

            // Verificamos permiso para Swing antes de intentar entrar
            if (CanEnter(Swing) && _input.IsSpaceHeld() && _ctx.TryGetSwingTarget(out _))
            {
                _sm.ChangeState(Swing);
                return;
            }

            if (!_sm.IsCurrent(Fall))
                _sm.ChangeState(Fall);
            return;
        }

        // --- LÓGICA SUELO (ACCIONES) ---

        // Espacio (Presionado único): QuickTravel o Smash
        if (_input.ConsumeSpaceDown())
        {
            if (CanEnter(QuickTravel) && _ctx.TryGetQuickTravel(_ctx.Tf, out _))
            {
                _sm.ChangeState(QuickTravel);
                return;
            }

            if (CanEnter(Smash) && _sm.ChangeState(Smash)) return;
        }

        // Espacio (Sostenido): Swing o Attract
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

        // Apuntado y Disparo
        if (_input.IsAimHeld() && CanEnter(Shoot))
        {
            if (_input.ConsumeShootDown())
            {
                if (_ctx.IsAimValid)
                    _sm.ChangeState(Shoot);
                else
                    _sm.ChangeState(Aim);

                return;
            }

            if (!_sm.IsCurrent(Shoot))
                _sm.ChangeState(Aim);

            return;
        }

        // Soltar venda
        if (_input.ConsumeDropDown() && CanEnter(DropBandage))
        {
            if (_sm.ChangeState(DropBandage)) return;
        }

        // Empujar (Push) requiere movimiento y detección
        if (moving && CanEnter(Push) && _ctx.TryGetPushTarget(out _, out _, out _))
        {
            if (_sm.IsCurrent(Push)) return;
            if (_sm.ChangeState(Push)) return;
        }

        // Movimiento base
        if (moving) _sm.ChangeState(Walk);
        else _sm.ChangeState(Idle);
    }

    private bool CanEnter(PlayerEnum.PlayerStateId state) => _ctx.Model.CanUseAbility(state);

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