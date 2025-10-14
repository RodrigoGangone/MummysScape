using UnityEngine;

/// <summary>
/// SwingState
/// - Activa/desactiva el spring al hook y la cuerda visual (LineRenderer en PlayerView).
/// - Permite control de movimiento/rotación mientras se hace swing, pero al 25% de la
///   velocidad/turning del size actual (50% menor que Walk/size).
/// - Mantiene el update visual de velocidad acorde.
/// </summary>
public class SwingState : State
{
    private readonly PlayerContext _ctx;
    private Transform _hookTf;

    // 50% menos => 50% del valor base
    private const float SwingMultiplier = 0.50f;

    public SwingState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("SwingState - OnEnter");

        if (_ctx.TryGetSwingTarget(out var hookRb))
        {
            _hookTf = hookRb.transform;

            // Física
            _ctx.SwingHandler.SetSpring(true);
            _ctx.SwingHandler.SpringJoint.connectedBody = hookRb;

            // Visual (cuerda)
            _ctx.View?.SetSwingLineActive(true, _hookTf);
        }
        else
        {
            _ctx.SwingHandler.SetSpring(false);
            _ctx.View?.SetSwingLineActive(false);
        }
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        // Movimiento relativo a cámara con control reducido (25%).
        Vector2 mv = _ctx.Input.Move;
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);
        if (!(dir.sqrMagnitude > 0.0001f))
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        float move = _ctx.MoveSpeed * SwingMultiplier;
        float turn = _ctx.TurnSpeed * SwingMultiplier;

        Vector3 targetPos = _ctx.Rb.position + dir * (move * Time.fixedDeltaTime);
        _ctx.Rb.MovePosition(targetPos);

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, turn * Time.fixedDeltaTime);
        _ctx.Rb.MoveRotation(smoothRot);

        // Normalizo visualmente a 0.25
        _ctx.View?.SetMoveSpeedVisual(SwingMultiplier);
    }

    public override void OnExit()
    {
        Debug.Log("SwingState - OnExit");
        _ctx.SwingHandler.SetSpring(false);
        _ctx.View?.SetSwingLineActive(false);
        _hookTf = null;
    }
}