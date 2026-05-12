using UnityEngine;
using static PlayerEnum;
using static PlayerEnum.PlayerStateId;

/// <summary>
/// Estado de feedback falso: reproduce una animación de intento fallido según la acción solicitada.
/// Para acciones continuas como Push, mantiene una simulación visual/motriz liviana hasta que
/// el jugador suelta el input o pierde contacto con el objetivo.
/// </summary>
public class FakeState : State
{
    private const float InputDeadZone = 0.05f;

    private readonly PlayerContext _ctx;

    private BoxPushAttract _pushTarget;
    private float _timer;
    private readonly float _maxDuration = 2.3f;
    private bool _isContinuous;

    public FakeState(PlayerContext ctx)
    {
        _ctx = ctx;
    }

    public override void OnEnter()
    {
        _timer = 0f;
        _pushTarget = null;
        _isContinuous = _ctx.AttemptedState == Push;

        if (_isContinuous)
        {
            if (!_ctx.TryGetPushTarget(out _pushTarget, out _, out _))
            {
                StateMachine.ChangeState(Walk);
                return;
            }
        }
        else
        {
            GameEventManager.Instance.playerEvents.OnLockRequested.Raise("FakeLock", true);
        }

        _ctx.Feedback.Execute(_ctx.AttemptedState, _ctx.Model.Size, _ctx);

        var stateInstance = StateMachine.GetState(_ctx.AttemptedState);
        if (stateInstance is IFailableState failable)
        {
            failable.OnTransitionDenied(_ctx.Model.Size);
        }
    }

    public override void OnUpdate()
    {
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(Fall);
            return;
        }

        if (_isContinuous)
        {
            HandleContinuousPush();
            return;
        }

        HandleOneShotExit();
    }

    public override void OnFixedUpdate()
    {
        if (!_isContinuous) return;

        Vector2 input = _ctx.Input.Move;
        if (input.sqrMagnitude < InputDeadZone * InputDeadZone) return;

        Vector3 direction = _ctx.CameraRelativeDir(input.x, input.y);
        if (direction.sqrMagnitude < 0.0001f) return;

        float fakePushSpeed = _ctx.MoveSpeed * 0.5f;
        Vector3 delta = direction * (fakePushSpeed * Time.fixedDeltaTime);

        _ctx.Rb.MovePosition(_ctx.Rb.position + delta);

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion smoothRotation = Quaternion.Slerp(
            _ctx.Rb.rotation,
            targetRotation,
            _ctx.TurnSpeed * Time.fixedDeltaTime
        );

        _ctx.Rb.MoveRotation(smoothRotation);
    }

    public override void OnExit()
    {
        if (!_isContinuous)
        {
            GameEventManager.Instance.playerEvents.OnLockRequested.Raise("FakeLock", false);
        }

        _ctx.View.Animator.SetBool("FakePush", false);
        _pushTarget = null;
    }

    private void HandleContinuousPush()
    {
        Vector2 input = _ctx.Input.Move;

        if (input.sqrMagnitude < InputDeadZone * InputDeadZone)
        {
            StateMachine.ChangeState(Idle);
            return;
        }

        if (!_ctx.TryGetPushTarget(out var currentTarget, out _, out _) || currentTarget != _pushTarget)
        {
            StateMachine.ChangeState(Walk);
            return;
        }

        if (!_pushTarget.IsGroundedForPushAttract())
        {
            StateMachine.ChangeState(Walk);
        }
    }

    private void HandleOneShotExit()
    {
        _timer += Time.deltaTime;

        AnimatorStateInfo stateInfo = _ctx.View.Animator.GetCurrentAnimatorStateInfo(0);
        bool animationFinished = stateInfo.normalizedTime >= 1f && !_ctx.View.Animator.IsInTransition(0);

        if (animationFinished || _timer >= _maxDuration)
        {
            StateMachine.ChangeState(Idle);
        }
    }
}