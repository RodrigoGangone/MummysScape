using UnityEngine;
using static Animations.Player;
using static PlayerEnum;
using static PlayerEnum.PlayerStateId;
using static PlayerEnum.PlayerSize;

/// <summary>
/// Estado de feedback falso: reproduce una animación de intento fallido según la acción solicitada.
/// Para acciones continuas como Push, mantiene la animación de intento hasta que
/// el jugador suelta el input o pierde contacto con el objetivo.
/// </summary>
public class FakeState : State, IBandageRestrictor
{
    private const float InputDeadZone = 0.05f;
    private const float OneShotSafetyTimeout = 5f;

    private enum FakeMode
    {
        None,
        OneShot,
        ContinuousPush
    }

    private readonly PlayerContext _ctx;

    private BoxPushAttract _pushTarget;
    private float _timer;
    private FakeMode _mode;
    private PlayerStateId _attemptedState;
    private PlayerSize _entrySize;

    public FakeState(PlayerContext ctx)
    {
        _ctx = ctx;
    }

    public override void OnEnter()
    {
        _timer = 0f;
        _pushTarget = null;
        _attemptedState = _ctx.AttemptedState;
        _entrySize = _ctx.Model.Size;
        _mode = GetMode(_attemptedState, _entrySize);

        if (_mode == FakeMode.None || !HasConfiguredFeedback(_attemptedState, _entrySize))
        {
            StateMachine.ChangeState(Idle);
            return;
        }

        if (_mode == FakeMode.ContinuousPush && !TryStartContinuousPush())
        {
            StateMachine.ChangeState(Walk);
            return;
        }

        _ctx.Feedback.Execute(_attemptedState, _entrySize, _ctx);
    }

    public override void OnUpdate()
    {
        switch (_mode)
        {
            case FakeMode.ContinuousPush:
                HandleContinuousPush();
                break;
            case FakeMode.OneShot:
                HandleOneShotTimeout();
                break;
        }
    }

    public override void OnFixedUpdate()
    {
        if (_mode != FakeMode.ContinuousPush) return;

        Vector2 input = _ctx.Input.Move;
        if (input.sqrMagnitude < InputDeadZone * InputDeadZone) return;

        Vector3 direction = _ctx.CameraRelativeDir(input.x, input.y);
        if (direction.sqrMagnitude < 0.0001f) return;

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
        if (_mode == FakeMode.ContinuousPush)
        {
            SetAnimatorBoolIfExists(FAKE_PUSH, false);
        }

        if (_mode == FakeMode.OneShot)
        {
            _ctx.View.StopBandage();
        }

        _pushTarget = null;
        _mode = FakeMode.None;
    }

    public void CompleteOneShot()
    {
        if (_mode != FakeMode.OneShot || !StateMachine.IsCurrent(Fake)) return;

        StateMachine.ChangeState(Idle);
    }

    private void HandleContinuousPush()
    {
        if (!_ctx.IsGrounded())
        {
            StateMachine.ChangeState(Fall);
            return;
        }

        if (_ctx.Model.Size != Small)
        {
            StateMachine.ChangeState(Idle);
            return;
        }

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

    private void HandleOneShotTimeout()
    {
        _timer += Time.deltaTime;

        if (_timer >= OneShotSafetyTimeout)
        {
            Debug.LogWarning($"Fake animation '{_attemptedState}' did not call EndFake. Finishing by timeout.");
            CompleteOneShot();
        }
    }

    private bool TryStartContinuousPush()
    {
        if (!_ctx.TryGetPushTarget(out _pushTarget, out _, out _)) return false;

        SetAnimatorBoolIfExists(FAKE_PUSH, true);
        return true;
    }

    private bool HasConfiguredFeedback(PlayerStateId state, PlayerSize size)
    {
        return _ctx.Feedback != null && _ctx.Feedback.HasFeedback(state, size);
    }

    private static FakeMode GetMode(PlayerStateId state, PlayerSize size)
    {
        if (state is Swing or Attract) return FakeMode.OneShot;
        if (state == Push && size == Small) return FakeMode.ContinuousPush;
        return FakeMode.None;
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        Animator animator = _ctx.View.Animator;
        if (animator == null) return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }
}
