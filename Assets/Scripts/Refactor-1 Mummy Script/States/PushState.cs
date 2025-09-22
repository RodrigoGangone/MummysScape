using UnityEngine;
using static PlayerEnum;

/// <summary>
/// PushState
/// Mantiene el ciclo de empuje: valida contacto en InteractionRuntime,
/// proyecta el input al eje permitido (±X o ±Z), traslada la caja con
/// BoxPushAttract (TryMoveAlongAxis) respetando bloqueos frontales y mantiene
/// al jugador centrado frente a la cara.
/// </summary>
public sealed class PushState : State
{
    private const float SnapSmoothTime = 0.08f;
    private const float SnapMaxSpeed = 5f;
    private readonly PlayerContext _ctx;
    private PushInfo _current;
    private Vector2 _snapVelocity;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        if (!_ctx.TryGetCachedPush(out _current))
        {
            if (!_ctx.TryGetPushInfo(_ctx.Input.Move, out _current))
            {
                StateMachine.ChangeState(PlayerStateId.Idle);
                return;
            }
        }

        _snapVelocity = Vector2.zero;
        SnapToAnchor(_current, _current.PlayerAnchor, false);
        AlignRotation(_current, true);
        _ctx.View?.SetMoveSpeedVisual(0f);
    }

    public override void OnExit()
    {
        _ctx.View?.SetMoveSpeedVisual(0f);
        _ctx.ClearPushCache();
        _snapVelocity = Vector2.zero;
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        Vector2 move = _ctx.Input.Move;
        if (!_ctx.TryGetPushInfo(move, out var info))
        {
            StateMachine.ChangeState(PlayerStateId.Idle);
            return;
        }

        _current = info;
        AlignRotation(info, false);
        ApplyPush(move, info);
    }

    private void ApplyPush(Vector2 rawInput, in PushInfo info)
    {
        var rb = _ctx.Rb;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 worldDir = _ctx.CameraRelativeDir(rawInput.x, rawInput.y);
        Vector3 horizontalDir = Vector3.ProjectOnPlane(worldDir, Vector3.up);
        float inputMagnitude = Mathf.Clamp01(rawInput.magnitude);

        if (horizontalDir.sqrMagnitude < 0.0001f || inputMagnitude < 0.01f)
        {
            SnapToAnchor(info, info.PlayerAnchor, false);
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        horizontalDir.Normalize();
        float forwardDot = Vector3.Dot(horizontalDir, -info.FaceNormal);
        if (forwardDot <= 0.1f)
        {
            SnapToAnchor(info, info.PlayerAnchor, false);
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        float pushStrength = inputMagnitude * Mathf.Clamp01(forwardDot);
        float distance = _ctx.MoveSpeed * pushStrength * Time.fixedDeltaTime;
        if (distance <= 0f)
        {
            SnapToAnchor(info, info.PlayerAnchor, false);
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        if (info.Target.TryMoveAlongAxis(info.MoveAxis, distance, out var boxDisplacement))
        {
            SnapToAnchor(info, info.PlayerAnchor + boxDisplacement, false);
            float travelledRatio = boxDisplacement.sqrMagnitude > 0f
                ? Mathf.Clamp01(boxDisplacement.magnitude / distance)
                : 0f;
            _ctx.View?.SetMoveSpeedVisual(pushStrength * travelledRatio);
        }
        else
        {
            SnapToAnchor(info, info.PlayerAnchor, false);
            _ctx.View?.SetMoveSpeedVisual(0f);
        }
    }

    private void AlignRotation(in PushInfo info, bool instant)
    {
        var rb = _ctx.Rb;
        Vector3 forward = info.BoxHorizontalCenter - rb.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = -info.FaceNormal;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = rb.transform.forward;
        }

        Quaternion targetRot = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Quaternion result = instant
            ? targetRot
            : Quaternion.Slerp(rb.rotation, targetRot, Mathf.Clamp01(_ctx.TurnSpeed * Time.fixedDeltaTime));
        rb.MoveRotation(result);
    }

    private void SnapToAnchor(in PushInfo info, Vector3 anchor, bool instant)
    {
        var rb = _ctx.Rb;
        Vector3 currentPos = rb.position;
        Vector3 target = new(anchor.x, currentPos.y, anchor.z);
        Vector2 currentXZ = new(currentPos.x, currentPos.z);
        Vector2 targetXZ = new(target.x, target.z);

        Vector2 nextXZ;
        if (instant)
        {
            _snapVelocity = Vector2.zero;
            nextXZ = targetXZ;
        }
        else
        {
            nextXZ = Vector2.SmoothDamp(
                currentXZ,
                targetXZ,
                ref _snapVelocity,
                SnapSmoothTime,
                SnapMaxSpeed,
                Time.fixedDeltaTime);

            if ((targetXZ - nextXZ).sqrMagnitude <= 0.0004f)
            {
                nextXZ = targetXZ;
                _snapVelocity = Vector2.zero;
            }
        }

        Vector3 next = new(nextXZ.x, currentPos.y, nextXZ.y);
        rb.MovePosition(next);
    }
}
