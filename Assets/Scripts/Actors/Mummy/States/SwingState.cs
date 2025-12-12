using UnityEngine;

public class SwingState : State
{
    private readonly PlayerContext _ctx;
    
    // Referencia al script del objeto que golpeamos
    private WrapHandler _currentWrapHandler;

    // Tiempos
    private float _timeReachedTarget; // Cuándo termina de viajar la línea e impacta

    // Banderas
    private bool _hasConnected; // Controla si ya iniciamos Wrap + Física

    public SwingState(PlayerContext ctx) => _ctx = ctx;

    public override void OnEnter()
    {
        Debug.Log("SwingState - Enter");
        
        // Reset
        _hasConnected = false;
        _currentWrapHandler = null;

        // 1. Obtener Target
        if (!_ctx.TryGetSwingTarget(out Rigidbody hookRb))
        {
            StateMachine.ChangeState(PlayerEnum.PlayerStateId.Fall);
            return;
        }

        // 2. Buscar WrapHandler
        _currentWrapHandler = hookRb.GetComponent<WrapHandler>();

        // 3. Iniciar Visuales (Cuerda viajando)
        _ctx.SwingHandler.StartVisuals(hookRb, hookRb.worldCenterOfMass);

        // 4. Calcular Tiempos
        // Solo nos importa cuándo llega la cuerda a la pared. 
        // Ya no sumamos WrapDuration al tiempo de espera físico.
        float travelDuration = _ctx.SwingHandler.DrawDuration;
        _timeReachedTarget = Time.time + travelDuration; 
    }

    public override void OnExit()
    {
        // 1. Ejecutar UnWrap al soltar
        if (_currentWrapHandler != null)
        {
            _currentWrapHandler.UnWrap();
        }

        // 2. Limpiar física
        _ctx.SwingHandler.Detach();
        
        // 3. Limpiar referencias
        _currentWrapHandler = null;
        _hasConnected = false;
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate()
    {
        // ---------------------------------------------------------
        // FASE 1: VUELO (Esperar a que la línea llegue visualmente)
        // ---------------------------------------------------------
        if (Time.time < _timeReachedTarget)
        {
            // Gravedad normal mientras viaja la cuerda
            _ctx.Rb.AddForce(Physics.gravity, ForceMode.Acceleration);
            return; 
        }

        // ---------------------------------------------------------
        // FASE 2: IMPACTO (Wrap Visual + Conexión Física SIMULTÁNEAS)
        // ---------------------------------------------------------
        if (!_hasConnected)
        {
            // A. Iniciar animación del Shader (Visual)
            if (_currentWrapHandler != null)
            {
                _currentWrapHandler.Wrap();
            }

            // B. Activar Joint (Física) INMEDIATAMENTE
            _ctx.SwingHandler.EnablePhysics(_ctx.Rb);
            
            _hasConnected = true;
        }

        // ---------------------------------------------------------
        // FASE 3: BALANCEO (Lógica de movimiento)
        // ---------------------------------------------------------
        
        var joint = _ctx.SwingHandler.SpringJoint;
        if (joint == null) return; 

        var rb = _ctx.Rb;
        Vector2 mv = _ctx.Input.Move;
        bool hasInput = mv.sqrMagnitude > 0.0001f;

        Vector3 wishDir = _ctx.CameraRelativeDir(mv.x, mv.y);
        Vector3 ropeDir = _ctx.SwingHandler.GetRopeDirWorld();

        Vector3 tanDir = Vector3.ProjectOnPlane(wishDir, ropeDir).normalized;

        Vector3 currentVel = rb.velocity;
        Vector3 currentTanVel = Vector3.ProjectOnPlane(currentVel, ropeDir);
        float currentSpeed = currentTanVel.magnitude;

        if (hasInput && tanDir.sqrMagnitude > 0f)
        {
            bool isFightingGravity = tanDir.y > 0;
            float forcePower = isFightingGravity ? 4f : 20f;
            float maxSpeed = _ctx.SwingHandler.MaxTangentialSpeed;

            float alignment = currentTanVel.sqrMagnitude > 0.0001f
                ? Vector3.Dot(tanDir, currentTanVel.normalized)
                : 0f;

            if (currentSpeed < maxSpeed || alignment < 0.5f)
            {
                rb.AddForce(tanDir * forcePower, ForceMode.Acceleration);
            }
        }
        else
        {
            _ctx.SwingHandler.HandlePassiveReturn(rb, Time.fixedDeltaTime);
        }

        _ctx.SwingHandler.ClampTangentialSpeed(rb);

        // Rotación visual
        Vector3 face = new Vector3(currentTanVel.x, 0f, currentTanVel.z);
        if (face.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, 12f * Time.fixedDeltaTime));
        }

        float n = Mathf.Clamp01(currentSpeed / Mathf.Max(0.01f, _ctx.SwingHandler.MaxTangentialSpeed));
        _ctx.View?.SetMoveSpeedVisual(n);
    }
}