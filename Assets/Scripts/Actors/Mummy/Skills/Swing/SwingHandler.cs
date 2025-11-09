using UnityEngine;
using System.Collections; // Necesario para la Corrutina

/// <summary>
/// SwingHandler (versión actualizada)
/// - Maneja la física del SpringJoint (Attach/Detach).
/// - Maneja el control tangencial (AddForce, Clamp, Retorno Pasivo).
/// - Maneja los visuales (LineRenderer y animación del material).
/// </summary>
[DisallowMultipleComponent]
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
    [SerializeField, Min(0f)] private float _tangentialAcceleration = 10f;

    [Header("Retorno Pasivo (sin input)")]
    
    [SerializeField] private bool _useGravityAssist = true;
    [SerializeField, Min(0f)] private float _gravityAssist = 2f;
    [SerializeField, Min(0f)] private float _tangentialBrake = 5f;

    [Header("Visuals (Line Renderer)")]
    
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Material _hookMaterial;
    [SerializeField] private Transform _lineStartPoint;
    [SerializeField, Range(0.1f, 0.5f)] private float _drawDuration;
    
    private readonly float _materialStartValue = 1.5f;
    private readonly float _materialEndValue = -1.5f;
    
    public float MaxTangentialSpeed => _maxTangentialSpeed;
    public float TangentialAccel    => _tangentialAcceleration;

    private Rigidbody _hook;
    private Rigidbody _playerRb; // Cacheamos el Rb del player
    
    private Coroutine _drawCoroutine;
    private int _thresholdPropertyID;
    private const string THRESHOLD_PROPERTY_NAME = "_rightThreshold"; // De tu código viejo
    
    private void Awake()
    {
        // Cacheamos el ID del shader para mejor rendimiento
        _thresholdPropertyID = Shader.PropertyToID(THRESHOLD_PROPERTY_NAME);

        // Aseguramos estado inicial limpio
        if (_hookMaterial)
            _hookMaterial.SetFloat(_thresholdPropertyID, _materialStartValue);
        if (_lineRenderer)
            _lineRenderer.enabled = false;
    }

    private void OnDisable()
    {
        // Limpieza si el objeto se deshabilita
        Detach();
    }
    
    private void LateUpdate()
    {
        // Actualizamos la posición del LineRenderer si estamos enganchados
        // Usamos LateUpdate para que se dibuje después de todas las físicas y animaciones
        if (SpringJoint && _lineRenderer && _lineRenderer.enabled && _lineStartPoint && _hook)
        {
            Vector3 worldHookPoint = _hook.transform.TransformPoint(SpringJoint.connectedAnchor);
            _lineRenderer.SetPosition(0, _lineStartPoint.position);
            _lineRenderer.SetPosition(1, worldHookPoint);
        }
    }
    
    public void Attach(Rigidbody playerRb, Rigidbody hookRb, Vector3 worldHookPoint)
    {
        if (!playerRb || !hookRb) return;

        _hook = hookRb;
        _playerRb = playerRb; // Cacheamos el Rb del player

        if (!SpringJoint)
            SpringJoint = playerRb.gameObject.AddComponent<SpringJoint>();

        SpringJoint.autoConfigureConnectedAnchor = false;
        SpringJoint.connectedBody = hookRb;
        SpringJoint.connectedAnchor = hookRb.transform.InverseTransformPoint(worldHookPoint);
        SpringJoint.anchor = Vector3.zero;

        float dMin = Mathf.Min(_minDistance, _maxDistance);
        float dMax = Mathf.Max(_minDistance, _maxDistance);
        SpringJoint.minDistance = dMin;
        SpringJoint.maxDistance = dMax;

        SpringJoint.spring   = _spring;
        SpringJoint.damper   = _damper;
        SpringJoint.massScale = _massScale;

        // --- Visuals OnEnter ---
        if (_lineRenderer)
            _lineRenderer.enabled = true;
        
        if (_hookMaterial)
        {
            // Detenemos cualquier corrutina anterior y empezamos la de dibujado
            if (_drawCoroutine != null)
                StopCoroutine(_drawCoroutine);
            _drawCoroutine = StartCoroutine(AnimateMaterialDraw());
        }
    }

    public void Detach()
    {
        // --- Physics OnExit ---
        if (SpringJoint)
        {
            Destroy(SpringJoint);
            SpringJoint = null;
        }
        _hook = null;
        _playerRb = null; // Limpiamos cache

        // --- Visuals OnExit ---
        if (_drawCoroutine != null)
        {
            StopCoroutine(_drawCoroutine);
            _drawCoroutine = null;
        }

        if (_lineRenderer)
            _lineRenderer.enabled = false;

        // Reseteamos el material a su valor inicial
        if (_hookMaterial)
            _hookMaterial.SetFloat(_thresholdPropertyID, _materialStartValue);
    }
    
    /// <summary>Dirección normalizada del cable desde el player hacia el hook.</summary>
    public Vector3 GetRopeDirWorld()
    {
        // Usamos el _playerRb cacheado
        if (!SpringJoint || !_playerRb || !_hook) return Vector3.up;
        Vector3 worldAnchor = _hook.transform.TransformPoint(SpringJoint.connectedAnchor);
        Vector3 fromPlayerToHook = worldAnchor - _playerRb.worldCenterOfMass;
        return fromPlayerToHook.sqrMagnitude > 1e-4f ? fromPlayerToHook.normalized : Vector3.up;
    }

    /// <summary>Clampea sólo la componente tangencial de la velocidad (conserva la radial).</summary>
    public void ClampTangentialSpeed(Rigidbody rb)
    {
        if (!SpringJoint || !rb) return;
        Vector3 ropeDir = GetRopeDirWorld(); // Usamos la versión sin parámetros
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

        Vector3 ropeDir = GetRopeDirWorld(); // Usamos la versión sin parámetros

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

    /// <summary>
    /// Anima el material del LineRenderer desde el valor inicial al final.
    /// (Reemplaza la corrutina 'Bandage' de SM_Hook)
    /// </summary>
    private IEnumerator AnimateMaterialDraw()
    {
        // Reseteamos al valor inicial antes de empezar
        _hookMaterial.SetFloat(_thresholdPropertyID, _materialStartValue);
        
        float time = 0f;
        while (time < _drawDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _drawDuration); // t va de 0 a 1
            float newValue = Mathf.Lerp(_materialStartValue, _materialEndValue, t);
            _hookMaterial.SetFloat(_thresholdPropertyID, newValue);
            yield return null;
        }
        
        // Aseguramos el valor final
        _hookMaterial.SetFloat(_thresholdPropertyID, _materialEndValue);
        _drawCoroutine = null; // Marcamos la corrutina como finalizada
    }
}