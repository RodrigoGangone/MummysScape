using UnityEngine;

/// <summary>
/// SwingState
/// - Conecta el spring al hook real (sin frames con ancla en (0,0,0)).
/// - Movimiento por fuerzas: input proyectado al plano tangencial del cable (AddForce Acceleration).
/// - Clamp de velocidad tangencial + brake cuando no hay input.
/// - Rotación suave mirando la dirección de la velocidad tangencial.
/// </summary>
public class SwingState : State
{
    private readonly PlayerContext _ctx;
    private Transform _hookTf;

    public SwingState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("SwingState - Enter");
        // Buscar hook válido, si no hay salimos (evita joints mal configurados).
        if (_ctx.TryGetSwingTarget(out var hookRb))
        {
            _hookTf = hookRb.transform;

            // Punto de enganche en mundo: si tu hook tiene un child "Anchor", úsalo.
            Vector3 worldHookPoint = hookRb.worldCenterOfMass;
            _ctx.SwingHandler.Attach(_ctx.Rb, hookRb, worldHookPoint);

            // Visual
            _ctx.View?.SetSwingLineActive(true, _hookTf);
        }
        else
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Fall);
        }
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        var rb = _ctx.Rb;
        var joint = _ctx.SwingHandler.SpringJoint;
        if (joint == null) return;

        Vector2 mv = _ctx.Input.Move;
        bool hasInput = mv.sqrMagnitude > 0.0001f;

        // Dirección de input en mundo relativa a cámara
        Vector3 wishDir = _ctx.CameraRelativeDir(mv.x, mv.y);

        // Proyección al plano tangencial
        Vector3 ropeDir = _ctx.SwingHandler.GetRopeDirWorld(rb);
        Vector3 tanDir = Vector3.ProjectOnPlane(wishDir, ropeDir).normalized;

        // Fuerza tangencial con input (aceleración independiente de la masa)
        if (hasInput && tanDir.sqrMagnitude > 0f)
            rb.AddForce(tanDir * _ctx.SwingHandler.TangentialAccel, ForceMode.Acceleration);
        else
            _ctx.SwingHandler.HandlePassiveReturn(rb, Time.fixedDeltaTime); // <<< cambio clave

        // Clamp para evitar “boost infinito”
        _ctx.SwingHandler.ClampTangentialSpeed(rb);

        // Mirar según la velocidad tangencial (opcional)
        Vector3 v = rb.velocity;
        Vector3 vTan = v - Vector3.Project(v, ropeDir);
        Vector3 face = new Vector3(vTan.x, 0f, vTan.z);
        if (face.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, 12f * Time.fixedDeltaTime));
        }

        // UI velocidad (opcional)
        float n = Mathf.Clamp01(vTan.magnitude / Mathf.Max(0.01f, _ctx.SwingHandler.MaxTangentialSpeed));
        _ctx.View?.SetMoveSpeedVisual(n);
    }

    public override void OnExit()
    {
        _ctx.SwingHandler.Detach();
        _ctx.View?.SetSwingLineActive(false);
        _hookTf = null;
    }
}
