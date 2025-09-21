using UnityEngine;
using static PlayerEnum;

/// <summary>
/// PushState
/// Mantiene el ciclo de empuje: valida contacto en InteractionRuntime,
/// proyecta el input al eje permitido (±X o ±Z), traslada la caja con
/// BoxPushAttract y mantiene al jugador centrado frente a la cara.
/// </summary>
public sealed class PushState : State
{
    private const float SnapLerpSpeed = 12f;
    private readonly PlayerContext _ctx;
    private PushInfo _current;

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

        ForceSnap(_current);
        AlignRotation(_current, true);
        _ctx.View?.SetMoveSpeedVisual(0f);
    }

    public override void OnExit()
    {
        _ctx.View?.SetMoveSpeedVisual(0f);
        _ctx.ClearPushCache();
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
        Vector3 displacement = info.MoveAxis * distance;

        info.Target.Move(displacement);
        SnapToAnchor(info, info.PlayerAnchor + displacement, false);
        _ctx.View?.SetMoveSpeedVisual(pushStrength);
    }

    private void AlignRotation(in PushInfo info, bool instant)
    {
        var rb = _ctx.Rb;
        Vector3 forward = -info.FaceNormal;
        if (forward.sqrMagnitude < 0.0001f) forward = rb.transform.forward;
        Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
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
        float t = instant ? 1f : Mathf.Clamp01(SnapLerpSpeed * Time.fixedDeltaTime);
        Vector3 next = Vector3.Lerp(currentPos, target, t);
        rb.MovePosition(next);
    }

    private void ForceSnap(in PushInfo info) => SnapToAnchor(info, info.PlayerAnchor, true);
}
