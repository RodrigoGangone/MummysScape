using UnityEngine;

public class FallState : State, IBandageRestrictor
{
    private readonly PlayerContext _ctx;

    // Reducción de velocidad en aire
    private const float AirMultiplier = 0.50f;

    // Qué tan rápido corrige la velocidad en el aire (Snappiness).
    // Valor alto (8-10) = Control muy responsivo, casi instantáneo (tipo arcade).
    // Valor bajo (1-3) = Se siente como "resbalar" en el aire, conserva más inercia.
    private const float AirControlForce = 5f;

    public FallState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        _ctx.View.Animator.SetBool("Fall", true);
        Debug.Log("FallState!");
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        Vector2 mv = _ctx.Input.Move;
        Vector3 dir = _ctx.CameraRelativeDir(mv.x, mv.y);

        // 1. Calcular la velocidad horizontal objetivo
        // Si no hay input, el objetivo es (0,0,0) para frenar suavemente en el aire
        float targetSpeed = _ctx.MoveSpeed * AirMultiplier;
        Vector3 targetVelXZ = dir * targetSpeed;

        // 2. Obtener velocidad actual (solo horizontal)
        Vector3 currentVel = _ctx.Rb.velocity;
        Vector3 currentVelXZ = new Vector3(currentVel.x, 0f, currentVel.z);

        // 3. Calcular la diferencia (fuerza necesaria)
        Vector3 diff = targetVelXZ - currentVelXZ;

        // OPTIONAL: Evitar frenado brusco si vienes con mucha velocidad del Swing.
        // Si el jugador va MUY rápido (más que su velocidad de aire), 
        // limitamos la fuerza de corrección para no frenarlo en seco, solo permitir girar.
        if (currentVelXZ.magnitude > targetSpeed && diff.magnitude > 0.1f)
        {
            // Reducimos la autoridad si vamos excedidos de velocidad para conservar momentum
            diff = Vector3.ClampMagnitude(diff, targetSpeed * 0.5f);
        }

        // 4. Aplicar fuerza solo en X y Z (ignorando Y para respetar gravedad)
        _ctx.Rb.AddForce(diff * AirControlForce, ForceMode.Acceleration);

        // 5. Rotación suave (Visual)
        if (currentVelXZ.sqrMagnitude > 0.5f) // Usar velocidad real para rotar, no input
        {
            Quaternion targetRot = Quaternion.LookRotation(currentVelXZ.normalized, Vector3.up);
            Quaternion smoothRot = Quaternion.Slerp(_ctx.Rb.rotation, targetRot,
                _ctx.TurnSpeed * AirMultiplier * Time.fixedDeltaTime);
            _ctx.Rb.MoveRotation(smoothRot);
        }

        // Visual UI
        _ctx.View?.SetMoveSpeedVisual(dir.sqrMagnitude > 0 ? AirMultiplier : 0f);
    }

    public override void OnExit()
    {
        _ctx.View.PlaySfx("Fall");
        _ctx.View.Animator.SetBool("Fall", false);
    }
}