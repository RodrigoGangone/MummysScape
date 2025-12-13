using UnityEngine;

public class SwingHandler : MonoBehaviour
{
    public SpringJoint SpringJoint { get; private set; }

    [Header("Cable (Physics)")]
    [SerializeField, Min(0f)] private float _minDistance = 2f;
    [SerializeField, Min(0f)] private float _maxDistance = 1.50f;
    [SerializeField, Range(0f, 200f)] private float _spring = 100f;
    [SerializeField, Range(0f, 30f)]  private float _damper = 12f;
    [SerializeField, Min(0.01f)]      private float _massScale = 45f;

    [Header("Control Tangencial")]
    [SerializeField, Min(0f)] private float _maxTangentialSpeed = 6f;

    [Header("Retorno Pasivo (sin input)")]
    [SerializeField] private bool _useGravityAssist = true;
    [SerializeField, Min(0f)] private float _gravityAssist = 2f;
    [SerializeField, Min(0f)] private float _tangentialBrake = 5f;

    // Propiedades públicas
    public float MaxTangentialSpeed => _maxTangentialSpeed;

    private Rigidbody _hookRb;
    private Rigidbody _playerRb;
    
    // Guardamos el punto local para saber dónde crear el Joint más tarde
    private Vector3 _currentLocalAnchor; 

    private void OnDisable() => Detach();

    // FASE 1: PREPARACIÓN (Solo guardamos datos físicos, la View dibuja)
    // Se llama en OnEnter del SwingState
    public void PreparePhysicsData(Rigidbody hookRb, Vector3 worldHookPoint)
    {
        _hookRb = hookRb;
        // Calculamos el punto local respecto al gancho
        _currentLocalAnchor = hookRb.transform.InverseTransformPoint(worldHookPoint);
    }

    // FASE 2: ACTIVACIÓN FÍSICA (Se llama cuando el State dice que pasó el tiempo)
    public void EnablePhysics(Rigidbody playerRb)
    {
        if (!playerRb || !_hookRb) return;
        _playerRb = playerRb;

        if (!SpringJoint)
            SpringJoint = playerRb.gameObject.AddComponent<SpringJoint>();

        SpringJoint.autoConfigureConnectedAnchor = false;
        SpringJoint.connectedBody = _hookRb;
        SpringJoint.connectedAnchor = _currentLocalAnchor; // Usamos el anchor guardado en PreparePhysicsData
        SpringJoint.anchor = Vector3.zero;

        // Configuración física
        float dMin = Mathf.Min(_minDistance, _maxDistance);
        float dMax = Mathf.Max(_minDistance, _maxDistance);
        SpringJoint.minDistance = dMin;
        SpringJoint.maxDistance = dMax;
        SpringJoint.spring = _spring;
        SpringJoint.damper = _damper;
        SpringJoint.massScale = _massScale;
    }

    public void Detach()
    {
        // Limpieza Física
        if (SpringJoint)
        {
            Destroy(SpringJoint);
            SpringJoint = null;
        }
        _playerRb = null;
        _hookRb = null;
    }

    // --- Helpers de Movimiento ---

    /// <summary>Dirección normalizada del cable desde el player hacia el hook.</summary>
    public Vector3 GetRopeDirWorld()
    {
        if (!_hookRb) return Vector3.up;

        // Calculamos la posición del gancho en el mundo usando el anchor local guardado
        Vector3 worldAnchor = _hookRb.transform.TransformPoint(_currentLocalAnchor);
        
        Vector3 playerPos = _playerRb ? _playerRb.worldCenterOfMass : transform.position;
        Vector3 dir = worldAnchor - playerPos;
        return dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.up;
    }

    public void ClampTangentialSpeed(Rigidbody rb)
    {
        if (!SpringJoint || !rb) return;

        Vector3 ropeDir = GetRopeDirWorld();
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

    public void HandlePassiveReturn(Rigidbody rb, float dt)
    {
        if (!SpringJoint || !rb) return;

        Vector3 ropeDir = GetRopeDirWorld();

        if (_useGravityAssist)
        {
            Vector3 gTan = Vector3.ProjectOnPlane(Physics.gravity, ropeDir);
            if (gTan.sqrMagnitude > 1e-6f)
                rb.AddForce(gTan * _gravityAssist, ForceMode.Acceleration);
        }

        Vector3 v = rb.velocity;
        Vector3 vRad = Vector3.Project(v, ropeDir);
        Vector3 vTan = v - vRad;

        vTan = Vector3.MoveTowards(vTan, Vector3.zero, _tangentialBrake * dt);
        rb.velocity = vRad + vTan;
    }
}