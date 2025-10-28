using UnityEngine;

/// <summary>
/// SwingHandler (versión mínima)
/// - Crea y destruye el SpringJoint apuntando SIEMPRE al hook real (sin frame en (0,0,0)).
/// - Expone helpers para dirección del cable y control tangencial (AddForce + clamp).
/// - Retorno pasivo cuando no hay input: gravedad tangencial + freno constante.
/// Cómo usar:
/// 1) Llamar Attach(rbPlayer, rbHook, puntoMundoHook) en OnEnter del SwingState.
/// 2) En FixedUpdate del SwingState: AddForce en plano tangencial (con input), 
///    HandlePassiveReturn (sin input) y ClampTangentialSpeed siempre al final.
/// 3) Llamar Detach() en OnExit.
/// </summary>
[DisallowMultipleComponent]
public class SwingHandler : MonoBehaviour
{
    public SpringJoint SpringJoint { get; private set; }

    // --- Cable (geometría y rigidez)
    [Header("Cable")]
    [SerializeField, Min(0f)] private float _minDistance = 2.25f;  // Si lo igualás con MaxDistance => cable tenso
    [SerializeField, Min(0f)] private float _maxDistance = 2.50f;
    [SerializeField, Range(0f, 200f)] private float _spring = 30f; // Rigidez del resorte
    [SerializeField, Range(0f, 30f)]  private float _damper = 16f; // Amortiguación (evita rebotar)
    [SerializeField, Min(0.01f)]      private float _massScale = 100f; // Escala de masa del lado del player en el solver

    // --- Control tangencial (input + seguridad)
    [Header("Control Tangencial")]
    [Tooltip("Límite de velocidad en el plano tangencial (evita boost infinito).")]
    [SerializeField, Min(0f)] private float _maxTangentialSpeed = 6f;
    [Tooltip("Aceleración aplicada con input a lo largo del plano tangencial.")]
    [SerializeField, Min(0f)] private float _tangentialAcceleration = 10f;

    // --- Retorno pasivo (sin input)
    [Header("Retorno Pasivo (sin input)")]
    [Tooltip("Proyecta la gravedad al plano tangencial para acelerar el retorno.")]
    [SerializeField] private bool _useGravityAssist = true;
    [Tooltip("Multiplicador de la gravedad tangencial (1 = gravedad pura).")]
    [SerializeField, Min(0f)] private float _gravityAssist = 2f;
    [Tooltip("Freno constante sobre la velocidad tangencial cuando no hay input.")]
    [SerializeField, Min(0f)] private float _tangentialBrake = 5f;

    // Exposición sólo de lo que el State necesita leer
    public float MaxTangentialSpeed => _maxTangentialSpeed;
    public float TangentialAccel    => _tangentialAcceleration;

    private Rigidbody _hook;

    public void Attach(Rigidbody playerRb, Rigidbody hookRb, Vector3 worldHookPoint)
    {
        if (!playerRb || !hookRb) return;

        _hook = hookRb;

        if (!SpringJoint)
            SpringJoint = playerRb.gameObject.AddComponent<SpringJoint>();

        SpringJoint.autoConfigureConnectedAnchor = false;
        SpringJoint.connectedBody = hookRb;
        SpringJoint.connectedAnchor = hookRb.transform.InverseTransformPoint(worldHookPoint);
        SpringJoint.anchor = Vector3.zero;

        // Distancias (si querés cable tenso => min=max desde el inspector)
        float dMin = Mathf.Min(_minDistance, _maxDistance);
        float dMax = Mathf.Max(_minDistance, _maxDistance);
        SpringJoint.minDistance = dMin;
        SpringJoint.maxDistance = dMax;

        SpringJoint.spring   = _spring;
        SpringJoint.damper   = _damper;
        SpringJoint.massScale = _massScale;

        // Nota: SpringJoint.enableCollision se dejó fuera a propósito (ver explicación).
    }

    public void Detach()
    {
        if (SpringJoint)
        {
            Destroy(SpringJoint);
            SpringJoint = null;
        }
        _hook = null;
    }

    /// <summary>Dirección normalizada del cable desde el player hacia el hook.</summary>
    public Vector3 GetRopeDirWorld(Rigidbody playerRb)
    {
        if (!SpringJoint || !playerRb || !_hook) return Vector3.up;
        Vector3 worldAnchor = _hook.transform.TransformPoint(SpringJoint.connectedAnchor);
        Vector3 fromPlayerToHook = worldAnchor - playerRb.worldCenterOfMass;
        return fromPlayerToHook.sqrMagnitude > 1e-4f ? fromPlayerToHook.normalized : Vector3.up;
    }

    /// <summary>Clampea sólo la componente tangencial de la velocidad (conserva la radial).</summary>
    public void ClampTangentialSpeed(Rigidbody rb)
    {
        if (!SpringJoint || !rb) return;
        Vector3 ropeDir = GetRopeDirWorld(rb);
        Vector3 v = rb.velocity;
        Vector3 vRad = Vector3.Project(v, ropeDir);
        Vector3 vTan = v - vRad;

        float maxTan = _maxTangentialSpeed;
        if (vTan.sqrMagnitude > maxTan * maxTan)
        {
            vTan = vTan.normalized * maxTan;
            rb.velocity = vRad + vTan;
        }
    }

    /// <summary>Retorno pasivo: gravedad tangencial opcional + freno tangencial constante.</summary>
    public void HandlePassiveReturn(Rigidbody rb, float dt)
    {
        if (!SpringJoint || !rb) return;

        Vector3 ropeDir = GetRopeDirWorld(rb);

        // Asistencia de gravedad (opcional)
        if (_useGravityAssist)
        {
            Vector3 gTan = Vector3.ProjectOnPlane(Physics.gravity, ropeDir);
            if (gTan.sqrMagnitude > 1e-6f)
                rb.AddForce(gTan * _gravityAssist, ForceMode.Acceleration);
        }

        // Freno tangencial constante
        Vector3 v = rb.velocity;
        Vector3 vRad = Vector3.Project(v, ropeDir);
        Vector3 vTan = v - vRad;

        vTan = Vector3.MoveTowards(vTan, Vector3.zero, _tangentialBrake * dt);
        rb.velocity = vRad + vTan;
    }
}
