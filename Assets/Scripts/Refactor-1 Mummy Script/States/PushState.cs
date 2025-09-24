using UnityEngine;
using PlayerStateId = PlayerEnum.PlayerStateId;

/// <summary>
/// PushState
/// Reduce la velocidad del jugador, mantiene la alineación contra la caja y delega el desplazamiento al BoxPushAttract.
/// Mientras los raycasts de InteractionRuntime sean válidos el player y la caja se mueven juntos sobre un eje X/Z.
/// </summary>
public sealed class PushState : State
{
    private const float PushSpeedFactor = 0.5f;

    private readonly PlayerContext _ctx;

    private BoxPushAttract _box;
    private BoxPushAttract.PushFace _face;
    private Vector3 _pushAxis;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        if (!TryBindBox())
        {
            StateMachine?.ChangeState(PlayerStateId.Idle);
            return;
        }

        SnapPlayerToFace();
        _ctx.View?.SetMoveSpeedVisual(0f);
    }

    public override void OnExit()
    {
        _box = null;
        _ctx.View?.SetMoveSpeedVisual(0f);
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        if (_box == null)
        {
            StateMachine?.ChangeState(PlayerStateId.Idle);
            return;
        }

        if (!_ctx.TryGetPushTarget(out var candidate, out var face) || candidate != _box)
        {
            StateMachine?.ChangeState(PlayerStateId.Idle);
            return;
        }

        if (face != _face)
        {
            _face = face;
            _pushAxis = _box.GetPushAxis(_face);
            if (_pushAxis.sqrMagnitude <= 0f)
            {
                StateMachine?.ChangeState(PlayerStateId.Idle);
                return;
            }

            SnapPlayerToFace();
        }

        Vector2 moveInput = _ctx.Input.Move;
        Vector3 desiredDir = _ctx.CameraRelativeDir(moveInput.x, moveInput.y);
        float axisFactor = Vector3.Dot(desiredDir, _pushAxis);
        if (axisFactor <= 0f)
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        axisFactor = Mathf.Clamp01(axisFactor);
        float playerSpeed = _ctx.MoveSpeed * PushSpeedFactor;
        float distance = playerSpeed * axisFactor * Time.fixedDeltaTime;
        if (!_box.TryMove(_face, distance))
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        Vector3 displacement = _pushAxis * distance;
        _ctx.Rb.MovePosition(_ctx.Rb.position + displacement);

        Quaternion targetRot = Quaternion.LookRotation(_pushAxis, Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, _ctx.TurnSpeed * Time.fixedDeltaTime);
        _ctx.Rb.MoveRotation(smoothRot);

        _ctx.View?.SetMoveSpeedVisual(axisFactor * PushSpeedFactor);
    }

    private bool TryBindBox()
    {
        if (!_ctx.TryGetPushTarget(out var box, out var face))
        {
            return false;
        }

        _box = box;
        _face = face;
        _pushAxis = _box.GetPushAxis(_face);
        return _pushAxis.sqrMagnitude > 0f;
    }

    private void SnapPlayerToFace()
    {
        if (_box == null || _pushAxis.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 playerPos = _ctx.Rb.position;
        Vector3 boxPos = _box.transform.position;

        if (Mathf.Abs(_pushAxis.z) > Mathf.Abs(_pushAxis.x))
        {
            playerPos.x = boxPos.x;
        }
        else
        {
            playerPos.z = boxPos.z;
        }

        _ctx.Rb.position = playerPos;
        _ctx.Rb.velocity = Vector3.zero;
        _ctx.Rb.angularVelocity = Vector3.zero;
        _ctx.Rb.rotation = Quaternion.LookRotation(_pushAxis, Vector3.up);
    }
}

