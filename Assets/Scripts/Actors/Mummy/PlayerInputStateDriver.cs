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
            if(!_sm.IsCurrent(KnockBack))
                _sm.ChangeState(KnockBack);
            
            return;
        }
        
        var mv = _input.Move;
        bool moving = Mathf.Abs(mv.x) > _moveDeadZone || Mathf.Abs(mv.y) > _moveDeadZone;

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

            if (_input.IsSpaceHeld() && _ctx.TryGetSwingTarget(out _))
            {
                _sm.ChangeState(Swing);
                return;
            }

            if (!_sm.IsCurrent(Fall))
                _sm.ChangeState(Fall);
            return;
        }

        if (_input.ConsumeSpaceDown())
        {
            if (_ctx.TryGetQuickTravel(_ctx.Tf, out _))
            {
                _sm.ChangeState(QuickTravel);
                return;
            }

            if (_sm.ChangeState(Smash)) return;
        }

        if (_input.IsSpaceHeld())
        {
            if (_sm.IsCurrent(Swing) || _sm.IsCurrent(Attract)) return;

            if (_ctx.TryGetSwingTarget(out _))
            {
                _sm.ChangeState(Swing);
                return;
            }

            if (_ctx.TryGetAttractTarget(out _)) 
            {
                _sm.ChangeState(Attract);
                return;
            }
        }

        if (_input.IsAimHeld())
        {
            if (_sm.IsCurrent(Shoot)) return;

            _sm.ChangeState(Aim);

            if (_input.ConsumeShootDown())
            {
                if (_ctx.IsAimValid)
                {
                    _sm.ChangeState(Shoot);
                }
                else
                {
                    Debug.Log("Bloqueado: El sistema dice que el tiro no es válido.");
                }
            }
            
            return;
        }
        if (_input.ConsumeDropDown())
        {
            if (_sm.ChangeState(DropBandage)) return;
        }

        if (moving && _ctx.TryGetPushTarget(out _, out _, out _))
        {
            if (_sm.IsCurrent(Push)) return;
            if (_sm.ChangeState(Push)) return;
        }

        if (moving) _sm.ChangeState(Walk);
        else _sm.ChangeState(Idle);
    }

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