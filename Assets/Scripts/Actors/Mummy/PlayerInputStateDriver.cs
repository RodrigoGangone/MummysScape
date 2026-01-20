using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlayerEnum.PlayerStateId;

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

    //TODO: ver que hacer con los "if" previos a pasar de state,ya que provocan entrar al state 1 vez por frame.
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

        // 1) Ambiente: caída/swing 
        if (!_ctx.IsGrounded())
        {
            // Si ya estoy en Swing: lo mantengo mientras se sostenga Space.
            if (_sm.IsCurrent(Swing))
            {
                if (!_input.IsSpaceHeld())
                {
                    _sm.ChangeState(Fall);
                }

                return; // No procesar otros estados en el aire.
            }

            // Permitir entrar a Swing desde Fall (con Space hold y target válido).
            if (_input.IsSpaceHeld() && _ctx.TryGetSwingTarget(out _))
            {
                _sm.ChangeState(Swing);
                return;
            }

            // Caso contrario: caer.
            if (!_sm.IsCurrent(Fall))
                _sm.ChangeState(Fall);
            return;
        }

        // 2) Space press => Smash (Head). Si el guard/SizeRules no dejan, sigue el flujo.
        if (_input.ConsumeSpaceDown())
        {
            if (_ctx.TryGetQuickTravel(_ctx.Tf, out _))
            {
                _sm.ChangeState(QuickTravel);
                return;
            }

            if (_sm.ChangeState(Smash)) return;
        }

        // 3) Space hold => Swing > Attract (según target frente)
        if (_input.IsSpaceHeld())
        {
            if (_sm.IsCurrent(Swing) || _sm.IsCurrent(Attract)) return;

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

// 4) Aim & Shoot
        if (_input.IsAimHeld())
        {
            // Protección: Si ya estamos disparando, no interrumpir
            if (_sm.IsCurrent(Shoot)) return;

            // Entramos a Aim (esto hará que AimState se ejecute y actualice IsAimValid en cada frame)
            _sm.ChangeState(Aim);

            // Si presionamos disparar...
            if (_input.ConsumeShootDown())
            {
                // ... YA NO CALCULAMOS NADA.
                // Solo preguntamos: ¿El último cálculo dijo que era válido?
                // Esto funciona perfecto con Joystick porque AimState ya hizo el trabajo sucio.
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

        // 5) Push si me estoy moviendo y tengo caja válida enfrente
        if (moving && _ctx.TryGetPushTarget(out _, out _, out _))
        {
            if (_sm.IsCurrent(Push)) return;
            if (_sm.ChangeState(Push)) return;
        }

        // 6) Walk / Idle
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