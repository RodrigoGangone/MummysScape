using UnityEngine;
using System.Collections;

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
    
    [Header("Visuals")]
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private Material _hookMaterial; // Material de la venda/gancho
    [SerializeField] private Transform _lineStartPoint; // La mano del player
    [SerializeField, Range(0f, 0.5f)] private float _drawDuration = 0.3f; // Tiempo de vuelo

    // Exponemos la duración para el State
    public float DrawDuration => _drawDuration; 
    public float MaxTangentialSpeed => _maxTangentialSpeed;

    private Rigidbody _hookRb;
    private Rigidbody _playerRb;
    
    // Guardamos el punto local para dibujar la línea ANTES de que exista el Joint
    private Vector3 _currentLocalAnchor; 
    private bool _visualsActive;

    private Coroutine _drawCoroutine;
    private int _thresholdPropertyID;
    private const string THRESHOLD_PROPERTY_NAME = "_rightThreshold";
    private readonly float _materialStartValue = 1.5f;
    private readonly float _materialEndValue = 0f;

    private void Awake()
    {
        _thresholdPropertyID = Shader.PropertyToID(THRESHOLD_PROPERTY_NAME);
        ResetVisuals();
    }

    private void OnDisable() => Detach();

    private void LateUpdate()
    {
        // DIBUJADO DE LA CUERDA
        // Debe funcionar si hay Joint (Física) O si solo están activas las visuales (Vuelo)
        if (_visualsActive && _lineRenderer && _lineStartPoint && _hookRb)
        {
            // Calculamos dónde está el punto de enganche en el mundo real ahora mismo
            Vector3 worldHookPoint = _hookRb.transform.TransformPoint(_currentLocalAnchor);
            
            _lineRenderer.SetPosition(0, _lineStartPoint.position);
            _lineRenderer.SetPosition(1, worldHookPoint);
        }
    }

    // FASE 1: VISUALES (Se llama al entrar al estado)
    public void StartVisuals(Rigidbody hookRb, Vector3 worldHookPoint)
    {
        _hookRb = hookRb;
        // Calculamos el punto local respecto al gancho y lo guardamos
        _currentLocalAnchor = hookRb.transform.InverseTransformPoint(worldHookPoint);
        
        _visualsActive = true;

        if (_lineRenderer) _lineRenderer.enabled = true;

        // Iniciar animación del shader
        if (_hookMaterial)
        {
            if (_drawCoroutine != null) StopCoroutine(_drawCoroutine);
            _drawCoroutine = StartCoroutine(AnimateMaterialDraw());
        }
    }

    // FASE 2: FÍSICA (Se llama cuando el State dice que pasó el tiempo)
    public void EnablePhysics(Rigidbody playerRb)
    {
        if (!playerRb || !_hookRb) return;
        _playerRb = playerRb;

        if (!SpringJoint)
            SpringJoint = playerRb.gameObject.AddComponent<SpringJoint>();

        SpringJoint.autoConfigureConnectedAnchor = false;
        SpringJoint.connectedBody = _hookRb;
        SpringJoint.connectedAnchor = _currentLocalAnchor; // Usamos el que calculamos en StartVisuals
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

        // Limpieza Visual
        if (_drawCoroutine != null) StopCoroutine(_drawCoroutine);
        _visualsActive = false;
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (_lineRenderer) _lineRenderer.enabled = false;
        if (_hookMaterial) _hookMaterial.SetFloat(_thresholdPropertyID, _materialStartValue);
    }

    // --- Helpers de Movimiento (Integrados del script viejo) ---

    /// <summary>Dirección normalizada del cable desde el player hacia el hook.</summary>
    public Vector3 GetRopeDirWorld()
    {
        // Si no hay hookRb, no hay dirección válida
        if (!_hookRb) return Vector3.up;

        // Calculamos la posición del gancho en el mundo usando el anchor local guardado
        Vector3 worldAnchor = _hookRb.transform.TransformPoint(_currentLocalAnchor);
        
        // Usamos la posición del player o la del transform si el player es null (fase visual)
        Vector3 playerPos = _playerRb ? _playerRb.worldCenterOfMass : transform.position;
        
        Vector3 dir = worldAnchor - playerPos;
        return dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.up;
    }

    /// <summary>Clampea sólo la componente tangencial de la velocidad.</summary>
    public void ClampTangentialSpeed(Rigidbody rb)
    {
        if (!SpringJoint || !rb) return;

        Vector3 ropeDir = GetRopeDirWorld();
        Vector3 v = rb.velocity;
        Vector3 vRad = Vector3.Project(v, ropeDir); // Velocidad en la dirección de la cuerda
        Vector3 vTan = v - vRad;                    // Velocidad perpendicular (tangencial)

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

        Vector3 ropeDir = GetRopeDirWorld();

        // 1. Asistencia de gravedad (opcional)
        // Ayuda a que el player baje al punto más bajo del péndulo más rápido
        if (_useGravityAssist)
        {
            Vector3 gTan = Vector3.ProjectOnPlane(Physics.gravity, ropeDir);
            if (gTan.sqrMagnitude > 1e-6f)
                rb.AddForce(gTan * _gravityAssist, ForceMode.Acceleration);
        }

        // 2. Freno tangencial constante
        // Evita que el player oscile eternamente
        Vector3 v = rb.velocity;
        Vector3 vRad = Vector3.Project(v, ropeDir);
        Vector3 vTan = v - vRad;

        vTan = Vector3.MoveTowards(vTan, Vector3.zero, _tangentialBrake * dt);
        rb.velocity = vRad + vTan;
    }

    private IEnumerator AnimateMaterialDraw()
    {
        _hookMaterial.SetFloat(_thresholdPropertyID, _materialStartValue);
        float time = 0f;
        while (time < _drawDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _drawDuration);

            float val = Mathf.Lerp(_materialStartValue, _materialEndValue, t);
            _hookMaterial.SetFloat(_thresholdPropertyID, val);
            yield return null;
        }
        _hookMaterial.SetFloat(_thresholdPropertyID, _materialEndValue);
    }
}