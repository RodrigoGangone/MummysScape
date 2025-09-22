using UnityEngine;
using static PlayerEnum;

/// <summary>
/// PushState
/// Mantiene el ciclo de empuje: proyecta el input al eje permitido, mueve la caja vía IPushable,
/// hace soft-snap lateral del player al centro horizontal de la cara y lo alinea mirando a la caja.
/// No gestiona las transiciones (eso lo hace el Driver); asume que al entrar hay un target válido.
/// </summary>
public sealed class PushState : State
{
    private const float SnapSmoothTime = 0.08f;
    private const float SnapMaxSpeed = 12f;

    private readonly PlayerContext _ctx;
    private PushInfo _pushInfo;
    private Vector3 _snapVelocity;
    private bool _hasInfo;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public void SetPushInfo(PushInfo info)
    {
        _pushInfo = info;
        _hasInfo = true;
    }

    public override void OnEnter()
    {
        SnapToAnchor(true);
        AlignToSurface();
    }

    public override void OnExit()
    {
        _snapVelocity = Vector3.zero;
        _hasInfo = false;
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        SnapToAnchor(false);
        MaintainFacing();
    }

    private void SnapToAnchor(bool instant)
    {
        if (_ctx?.Rb == null || !_hasInfo) return;

        Vector3 anchor = _pushInfo.Anchor;
        Vector3 current = _ctx.Rb.position;

        if (instant)
        {
            _ctx.Rb.MovePosition(anchor);
            _snapVelocity = Vector3.zero;
            return;
        }

        Vector3 next = Vector3.SmoothDamp(current, anchor, ref _snapVelocity, SnapSmoothTime, SnapMaxSpeed, Time.fixedDeltaTime);
        _ctx.Rb.MovePosition(next);
    }

    private void AlignToSurface()
    {
        if (_ctx?.Rb == null || !_hasInfo) return;

        Vector3 forward = -_pushInfo.FaceNormal;
        forward.y = 0f;
        if (!(forward.sqrMagnitude > 0.0001f)) return;

        Quaternion target = Quaternion.LookRotation(forward.normalized, Vector3.up);
        _ctx.Rb.MoveRotation(target);
    }

    private void MaintainFacing()
    {
        if (_ctx?.Rb == null || !_hasInfo) return;

        Vector3 forward = -_pushInfo.FaceNormal;
        forward.y = 0f;
        if (!(forward.sqrMagnitude > 0.0001f)) return;

        Quaternion target = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Quaternion smooth = Quaternion.Slerp(_ctx.Rb.rotation, target, _ctx.TurnSpeed * Time.fixedDeltaTime);
        _ctx.Rb.MoveRotation(smooth);
    }
}
