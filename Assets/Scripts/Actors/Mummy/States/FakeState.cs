using UnityEngine;
using UnityEngine.UIElements;
using static PlayerEnum;
using static PlayerEnum.PlayerStateId;

public class FakeState : State
{
    private PlayerContext _ctx;
    private float _timer;
    private float _maxDuration = 2.3f; // Duración por defecto para OneShots
    private bool _isContinuous;

    public FakeState(PlayerContext ctx)
    {
        _ctx = ctx;
    }

    public override void OnEnter()
    {
        _timer = 0;

        // 1. Identificar si es una acción continua (Push) o OneShot
        _isContinuous = (_ctx.AttemptedState == Push);

        if (!_isContinuous)
            GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Fake Lock", true);

        // 2. Ejecutar Feedback (Animación, Sonido, Partículas)
        _ctx.Feedback.Execute(_ctx.AttemptedState, _ctx.Model.Size, _ctx);

        // 3. Notificar al estado original si implementa IFailableState
        var stateInstance = StateMachine.GetState(_ctx.AttemptedState);
        if (stateInstance is IFailableState failable)
        {
            failable.OnTransitionDenied(_ctx.Model.Size);
        }
    }

    public override void OnUpdate()
    {
        // Interrupción por movimiento: si el jugador se aleja, salimos siempre

        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(Fall);
            return;
        }

        if (_isContinuous)
        {
            HandleContinuousExit();
        }
        else
        {
            HandleOneShotExit();
        }
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        if (!_isContinuous)
            GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Fake Lock", false);
    }

    private void HandleOneShotExit()
    {
        _timer += Time.deltaTime;
        var stateInfo = _ctx.View.Animator.GetCurrentAnimatorStateInfo(0);

        // Salida por fin de animación o por tiempo de seguridad
        bool animFinished = stateInfo.normalizedTime >= 1.0f && !_ctx.View.Animator.IsInTransition(0);

        if (animFinished || _timer >= _maxDuration)
        {
            StateMachine.ChangeState(Idle);
        }
    }

    private void HandleContinuousExit()
    {
        // En el caso del Push, el "Fake" dura mientras el jugador intente empujar 
        // pero no tenga el tamaño adecuado.

        bool isMoving = _ctx.Input.Move.magnitude > 0.1f;
        bool isTouchingPushable = _ctx.TryGetPushTarget(out _, out _, out _);

        // Si deja de caminar contra el objeto o pierde el contacto, termina el Fake
        if (!isMoving || !isTouchingPushable)
        {
            StateMachine.ChangeState(Idle);
        }
    }
}