using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlobShadow : MonoBehaviour
{
    [Header("Refs")]
    public DecalProjector projector;

    [Header("Detección de suelo")]
    public LayerMask groundMask = ~0;
    public float maxRayDistance = 10f;
    public float sphereCastRadius = 0.15f;
    public float surfaceOffset = 0.01f;
    public float groundedThreshold = 0.25f;

    [Header("Tamaño del decal (X/Y)")]
    public Vector2 sizeOnGround = new Vector2(1.0f, 1.0f);

    [Header("Proyección (Z)")]
    [Tooltip("Profundidad objetivo cuando el personaje está en el aire (size.z). En tierra se usa la profundidad original del projector.")]
    public float maxProjectionDepth = 0.4f; // Z en el aire

    [Range(0.3f, 1f)]
    public float xyShrinkInAir = 0.8f;

    [Header("Shader properties (cambiar si tu Reference es distinto)")]
    public string radiusProperty = "_Radius";

    [Header("Valores del shader")]
    public float groundRadius = 0.45f;
    public float airRadius = 0.12f;

    [Header("Suavizado")]
    [Range(0f, 60f)] public float followLerp = 20f;  // movimiento/orientación
    [Range(0f, 60f)] public float propsLerp  = 12f;  // transición Radius
    [Range(0f, 60f)] public float sizeLerp   = 12f;  // transición X/Y y Z

    // ==========================
    //   RENDERING LAYERS (A)
    // ==========================
    public enum AffectMode
    {
        // Afecta a todo menos al PJ (excluye un bit que setearás en los renderers del PJ)
        AffectAllExceptCharacter = 0,

        // Más estricta: sólo pinta donde haya “Ground bit” (el/los suelos marcados)
        OnlyGround = 1
    }

    [Header("Decal Filtering (Rendering Layers)")]
    public AffectMode affectMode = AffectMode.AffectAllExceptCharacter;

    [Tooltip("Bit 0..31 de Rendering Layer que usarás para EXCLUIR al personaje (AffectAllExceptCharacter).")]
    [Range(0, 31)] public int characterExcludeBit = 20;

    [Tooltip("Bit 0..31 de Rendering Layer que usarás para marcar el SUELO (OnlyGround).")]
    [Range(0, 31)] public int groundBit = 21;

    // Internos
    Transform _t;
    int _radiusID;
    bool _hasRadius;

    float _curRadius;
    Vector2 _curXY;

    // >>> Nuevos internos para Z (profundidad)
    float _baseProjectionDepth;  // Z original del DecalProjector (en tierra)
    float _curProjectionDepth;   // Z actual (interpolada)

    void Reset()
    {
        if (!projector) projector = GetComponentInChildren<DecalProjector>();
    }

    void Awake()
    {
        _t = transform;

        if (!projector)
        {
            projector = GetComponentInChildren<DecalProjector>();
            if (!projector)
            {
                Debug.LogWarning($"{nameof(BlobShadow)}: Asigná un DecalProjector hijo.");
                enabled = false;
                return;
            }
        }

        // Instanciar material para no modificar el asset compartido
        if (projector.material != null && !projector.material.name.EndsWith("(Instance)"))
            projector.material = Instantiate(projector.material);

        // Cachear IDs
        CacheShaderPropertyIDs();

        // --- Inicializar tamaños
        var s0 = projector.size;

        // Guardar la Z ORIGINAL del projector como profundidad "en tierra"
        _baseProjectionDepth  = Mathf.Max(0.001f, s0.z);
        _curProjectionDepth   = _baseProjectionDepth;

        // X/Y las controlamos por script (en tierra arrancamos en sizeOnGround)
        _curXY = sizeOnGround;
        projector.size = new Vector3(sizeOnGround.x, sizeOnGround.y, _baseProjectionDepth);

        // Inicializar valores del shader
        _curRadius = _hasRadius ? projector.material.GetFloat(_radiusID) : groundRadius;

        if (!projector.gameObject.activeSelf) projector.gameObject.SetActive(true);

        // >>> Aplicar máscara de Rendering Layers según modo elegido
        ApplyProjectorRenderingLayerMask();
    }

    void CacheShaderPropertyIDs()
    {
        _radiusID = Shader.PropertyToID(radiusProperty);
        _hasRadius = projector.material && projector.material.HasProperty(_radiusID);

        if (!_hasRadius && projector.material.HasProperty("_Radius"))
        {
            _radiusID = Shader.PropertyToID("_Radius");
            _hasRadius = true;
        }

        if (!_hasRadius)
            Debug.LogWarning($"{nameof(BlobShadow)}: No encuentro propiedad '{radiusProperty}' / '_Radius' en el material del Decal.");
    }

    void ApplyProjectorRenderingLayerMask()
    {
        if (!projector) return;

        switch (affectMode)
        {
            case AffectMode.AffectAllExceptCharacter:
            {
                uint excludeMask = 1u << Mathf.Clamp(characterExcludeBit, 0, 31);
                projector.renderingLayerMask = ~excludeMask; // afecta a todos EXCEPTO a los que tengan ese bit
                break;
            }
            case AffectMode.OnlyGround:
            {
                uint g = 1u << Mathf.Clamp(groundBit, 0, 31);
                projector.renderingLayerMask = g; // sólo pinta donde haya “suelo” con ese bit
                break;
            }
        }
    }

    void LateUpdate()
    {
        if (!projector) return;

        // --- 1) Ray al suelo
        Vector3 origin = _t.position + Vector3.up * 0.2f;
        bool hitFound = Physics.SphereCast(
            origin, sphereCastRadius, Vector3.down,
            out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore
        );

        if (!hitFound)
        {
            if (projector.gameObject.activeSelf) projector.gameObject.SetActive(false);
            return;
        }
        else if (!projector.gameObject.activeSelf)
        {
            projector.gameObject.SetActive(true);
        }

        float height = hit.distance;
        bool grounded = height <= groundedThreshold;

        // --- 2) Posición / orientación del projector
        Vector3 targetPos = hit.point + hit.normal * surfaceOffset;
        Vector3 forward = -hit.normal; // proyecta a lo largo de -Z
        Vector3 up = Vector3.ProjectOnPlane(
            _t.forward.sqrMagnitude > 0.001f ? _t.forward : Vector3.forward, hit.normal
        ).normalized;
        if (up.sqrMagnitude < 0.001f) up = Vector3.up;

        float kFollow = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        Transform pt = projector.transform;
        pt.position = Vector3.Lerp(pt.position, targetPos, kFollow);
        pt.rotation = Quaternion.Slerp(pt.rotation, Quaternion.LookRotation(forward, up), kFollow);

        // --- 3) Tamaño X/Y del decal (achicar en el aire)
        Vector2 targetXY = grounded ? sizeOnGround : sizeOnGround * xyShrinkInAir;
        float kSize = 1f - Mathf.Exp(-sizeLerp * Time.deltaTime);
        _curXY = Vector2.Lerp(_curXY, targetXY, kSize);

        // --- 4) Profundidad Z del projector:
        //      en tierra = Z original (_baseProjectionDepth)
        //      en el aire = maxProjectionDepth
        float targetZ = grounded ? _baseProjectionDepth : Mathf.Max(0.001f, maxProjectionDepth);
        _curProjectionDepth = Mathf.Lerp(_curProjectionDepth, targetZ, kSize);

        // Aplicar size completo
        var size = projector.size;
        size.x = _curXY.x;
        size.y = _curXY.y;
        size.z = _curProjectionDepth; // ¡sin clamp a max aquí!
        projector.size = size;

        // --- 5) Shader: Radius según grounded/air, con transición suave
        float targetRadius = grounded ? groundRadius : airRadius;
        float kProps = 1f - Mathf.Exp(-propsLerp * Time.deltaTime);
        _curRadius = Mathf.Lerp(_curRadius, targetRadius, kProps);
        ApplyShaderValues(_curRadius);
    }

    void ApplyShaderValues(float radius)
    {
        if (projector.material && _hasRadius)
            projector.material.SetFloat(_radiusID, radius);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (projector)
        {
            if (Application.isPlaying && projector.material != null && !projector.material.name.EndsWith("(Instance)"))
                projector.material = Instantiate(projector.material);

            // Asegurá que el Z actual del projector no colapse a 0
            var s = projector.size;
            s.z = Mathf.Max(0.001f, s.z);
            projector.size = s;

            // Reaplica máscara por si cambiaste bits o modo en el inspector
            ApplyProjectorRenderingLayerMask();
        }

        maxProjectionDepth = Mathf.Max(0.001f, maxProjectionDepth);
        groundedThreshold  = Mathf.Max(0f, groundedThreshold);
        sphereCastRadius   = Mathf.Max(0f, sphereCastRadius);
        surfaceOffset      = Mathf.Max(0f, surfaceOffset);
    }
#endif
}