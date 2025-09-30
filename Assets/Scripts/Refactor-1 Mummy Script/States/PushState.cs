using UnityEngine;
using PlayerStateId = PlayerEnum.PlayerStateId;

/// <summary>
/// PushState
/// Gestiona el empuje de cajas sobre un eje X/Z válido, priorizando control del jugador.
/// - No snapea posición contra la cara.
/// - Sale a Walk si tu input deja de acompañar el eje o mirás lejos del centro de la caja.
/// - Solo rota SUAVE hacia el centro horizontal de la caja cuando la caja realmente se mueve.
/// Cómo:
/// 1) Proyecta el input relativo a cámara sobre el eje de empuje (DOT).
/// 2) Si DOT es bajo (con input presente) o el ángulo de mirada al centro excede umbral → ChangeState(Walk).
/// 3) Si TryMove() de la caja avanza, el player acompaña en ese eje y rota suavemente al centro.
/// </summary>
public sealed class PushState : State
{
    private const float PushSpeedFactor = 0.5f;

    // Umbrales de salida y rotación
    private const float ExitAxisDotThreshold = 0.2f; // si el DOT(input,eje) cae por debajo con input → salir
    private const float ExitAngleDeg         = 25f;  // si mirás más lejos que esto del centro con input → salir
    private const float MinInputToExit       = 0.1f; // ruido mínimo para considerar intención

    private readonly PlayerContext _ctx;

    private BoxPushAttract _box;
    private BoxPushAttract.PushFace _face;
    private Vector3 _pushAxis;

    public PushState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("PushState");
        if (!TryBindBox())
        {
            StateMachine?.ChangeState(PlayerStateId.Idle);
            return;
        }

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

        // Revalidamos target/cara (no hacemos snap, solo actualizamos eje)
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
        }

        // Input relativo a cámara
        Vector2 moveInput = _ctx.Input.Move;
        float inputMag = moveInput.magnitude;

        Vector3 desiredDir = _ctx.CameraRelativeDir(moveInput.x, moveInput.y);
        float axisFactor = Vector3.Dot(desiredDir, _pushAxis);
        axisFactor = Mathf.Clamp01(axisFactor);

        // --- Reglas de SALIDA para no “bloquear” el giro/salida ---
        if (inputMag > MinInputToExit)
        {
            // 1) Input ya no acompaña el eje permitido
            if (axisFactor < ExitAxisDotThreshold)
            {
                StateMachine?.ChangeState(PlayerStateId.Walk);
                return;
            }

            // 2) Mirada se aleja mucho del centro de la caja
            if (IsAngleTooLarge(ExitAngleDeg))
            {
                StateMachine?.ChangeState(PlayerStateId.Walk);
                return;
            }
        }

        // Sin intención válida de empuje, quedate sin mover (pero no fuerces rotación)
        if (axisFactor <= 0f)
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        // Intentar mover primero la CAJA
        float playerSpeed = _ctx.MoveSpeed * PushSpeedFactor;
        float distance = playerSpeed * axisFactor * Time.fixedDeltaTime;

        if (!_box.TryMove(_face, distance))
        {
            _ctx.View?.SetMoveSpeedVisual(0f);
            return;
        }

        // Caja se movió → acompaño al player sobre el eje
        Vector3 displacement = _pushAxis * distance;
        _ctx.Rb.MovePosition(_ctx.Rb.position + displacement);

        // Rotación SUAVE al centro horizontal de la caja (solo si realmente empujamos)
        SmoothLookAtBoxCenter(axisFactor);

        _ctx.View?.SetMoveSpeedVisual(axisFactor * PushSpeedFactor);
    }

    private bool TryBindBox()
    {
        if (!_ctx.TryGetPushTarget(out var box, out var face))
            return false;

        _box = box;
        _face = face;
        _pushAxis = _box.GetPushAxis(_face);
        return _pushAxis.sqrMagnitude > 0f;
    }

    /// <summary>True si el ángulo al centro horizontal de la caja supera el umbral.</summary>
    private bool IsAngleTooLarge(float maxAngleDeg)
    {
        if (_box == null) return false;

        Vector3 playerPos = _ctx.Rb.position;
        Vector3 target = _box.transform.position;
        target.y = playerPos.y;

        Vector3 toBox = (target - playerPos);
        toBox.y = 0f;
        if (toBox.sqrMagnitude <= 1e-6f) return false;
        toBox.Normalize();

        Vector3 forward = _ctx.Rb.rotation * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 1e-6f) return false;
        forward.Normalize();

        float angle = Vector3.Angle(forward, toBox);
        return angle > maxAngleDeg;
    }

    /// <summary>
    /// Rotación suave hacia el centro horizontal de la caja, modulada por TurnSpeed y axisFactor.
    /// Solo se usa cuando el empuje realmente ocurrió (TryMove == true).
    /// </summary>
    private void SmoothLookAtBoxCenter(float axisFactor)
    {
        if (_box == null || axisFactor <= 0f)
            return;

        Vector3 playerPos = _ctx.Rb.position;
        Vector3 target = _box.transform.position;
        target.y = playerPos.y;

        Vector3 dir = target - playerPos;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 1e-6f)
            return;

        dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        float t = Mathf.Clamp01(_ctx.TurnSpeed * Time.fixedDeltaTime) * axisFactor;
        Quaternion smooth = Quaternion.Slerp(_ctx.Rb.rotation, targetRot, t);
        _ctx.Rb.MoveRotation(smooth);
    }
}