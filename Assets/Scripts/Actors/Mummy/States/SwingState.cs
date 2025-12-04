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

        // 1. Vectores básicos
        Vector3 wishDir = _ctx.CameraRelativeDir(mv.x, mv.y);
        Vector3 ropeDir = _ctx.SwingHandler.GetRopeDirWorld(); // Vector desde Player -> Hook
        
        // 2. Dirección tangencial deseada (A dónde queremos ir)
        Vector3 tanDir = Vector3.ProjectOnPlane(wishDir, ropeDir).normalized;

        // 3. Velocidad actual en el plano del swing
        Vector3 currentVel = rb.velocity;
        Vector3 currentTanVel = Vector3.ProjectOnPlane(currentVel, ropeDir);
        float currentSpeed = currentTanVel.magnitude;

        // 4. LÓGICA DE FUERZA DE PÉNDULO
        if (hasInput && tanDir.sqrMagnitude > 0f)
        {
            // A) Detectar si estamos tratando de "trepar" o "bajar"
            // tanDir.y > 0 significa que el input apunta hacia el cielo (luchar contra gravedad)
            bool isFightingGravity = tanDir.y > 0;

            // B) Configurar fuerzas
            // Si bajamos, ayudamos mucho (Gravedad + Input).
            // Si subimos, ayudamos muy poco o nada (Input vs Gravedad).
            float forcePower = isFightingGravity ? 4f : 20f; 
            
            // C) Límite de Velocidad "Suave" (Soft Cap)
            // Si ya vamos más rápido que el máximo, NO empujamos más en esa dirección.
            // Esto evita la aceleración infinita sin frenarte de golpe.
            float maxSpeed = _ctx.SwingHandler.MaxTangentialSpeed;
            
            // Calculamos si el input está alineado con la velocidad actual
            float alignment = Vector3.Dot(tanDir, currentTanVel.normalized);

            // Solo aplicamos fuerza si estamos bajo el límite O si estamos girando (cambiando dirección)
            if (currentSpeed < maxSpeed || alignment < 0.5f)
            {
                rb.AddForce(tanDir * forcePower, ForceMode.Acceleration);
            }
        }
        else
        {
            // Freno pasivo suave (Drag) para que no oscile eternamente
            _ctx.SwingHandler.HandlePassiveReturn(rb, Time.fixedDeltaTime);
        }

        // Clamp de seguridad final (por si la gravedad física te acelera demasiado en una caída muy larga)
        // Puedes relajar esto un poco para permitir picos de velocidad en caídas grandes.
        _ctx.SwingHandler.ClampTangentialSpeed(rb);

        // --- Rotación y Visuales (Igual que antes) ---
        Vector3 face = new Vector3(currentTanVel.x, 0f, currentTanVel.z);
        if (face.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, 12f * Time.fixedDeltaTime));
        }

        float n = Mathf.Clamp01(currentSpeed / Mathf.Max(0.01f, _ctx.SwingHandler.MaxTangentialSpeed));
        _ctx.View?.SetMoveSpeedVisual(n);
    }

    public override void OnExit()
    {
        _ctx.SwingHandler.Detach();
        _ctx.View?.SetSwingLineActive(false);
        _hookTf = null;
    }
}
