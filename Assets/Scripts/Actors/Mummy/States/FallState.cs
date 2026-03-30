using UnityEngine;
using static Animations.Player;

/// <summary> 
/// Estado de Caída: Controla el movimiento del personaje mientras está en el aire, aplicando un 
/// multiplicador de control reducido para permitir maniobras limitadas antes de tocar suelo. 
/// </summary>

public class FallState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;

    private const float AirMultiplier = 0.50f;
    private const float AirControlForce = 5f;

    public FallState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool(FALL, true);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        Vector2 mv = _ctx.Input.Move;
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);

        float targetSpeed = _ctx.MoveSpeed * AirMultiplier;
        Vector3 targetVelXZ = dir * targetSpeed;

        Vector3 currentVel = _ctx.Rb.linearVelocity;
        Vector3 currentVelXZ = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 diff = targetVelXZ - currentVelXZ;

        if (currentVelXZ.magnitude > targetSpeed && diff.magnitude > 0.1f)
            diff = Vector3.ClampMagnitude(diff, targetSpeed * 0.5f);

        _ctx.Rb.AddForce(diff * AirControlForce, ForceMode.Acceleration);

        if (currentVelXZ.sqrMagnitude > 0.5f) 
        {
            Quaternion targetRot = Quaternion.LookRotation(currentVelXZ.normalized, Vector3.up);
            Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot,
                _ctx.TurnSpeed * AirMultiplier * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smoothRot);
        }

        _ctx.View?.SetMoveSpeedVisual(dir.sqrMagnitude > 0 ? AirMultiplier : 0f);
    }

    public override void OnExit()
    {
        _ctx.View.PlaySfx("Fall");
        _ctx.View.Animator.SetBool(FALL, false);
    }
}