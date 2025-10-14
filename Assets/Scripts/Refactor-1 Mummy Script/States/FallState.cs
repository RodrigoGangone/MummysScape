using UnityEngine;

/// <summary>
/// FallState
/// - Control aéreo y rotación reducidos al 50% (50% menor) respecto del size actual.
/// - No anula gravedad: sólo suma un desplazamiento horizontal limitado y orienta suavemente.
/// - Mantiene feedback visual de velocidad acorde al control disponible.
/// </summary>
public class FallState : State
{
    private readonly PlayerContext _ctx;

    // 50% menos => 50% del valor base
    private const float AirMultiplier = 0.50f;

    public FallState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()  { Debug.Log("FallState!"); }
    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        // Control horizontal aéreo reducido (no tocamos Y: gravedad la maneja el Rigidbody/Physics).
        Vector2 mv = _ctx.Input.Move;
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);

        if (dir.sqrMagnitude <= 0.0001f)
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        float move = _ctx.MoveSpeed * AirMultiplier;
        float turn = _ctx.TurnSpeed * AirMultiplier;

        // Sólo desplazamiento en plano XZ (Y queda para la física).
        Vector3 horizontal = new Vector3(dir.x, 0f, dir.z);
        Vector3 targetPos = _ctx.Rb.position + horizontal * (move * Time.fixedDeltaTime);
        _ctx.Rb.MovePosition(targetPos);

        // Giro suave reducido
        if (horizontal.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(horizontal, Vector3.up);
            Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, turn * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smoothRot);
        }

        _ctx.View?.SetMoveSpeedVisual(AirMultiplier);
    }

    public override void OnExit() { }
}