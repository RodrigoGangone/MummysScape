using System.Collections;
using UnityEngine;

/// <summary>
/// Controla un pilar de fuego individual.
/// Mantiene el fuego apagado o encendido, reproduce el efecto inicial de ignición con chispas/destello
/// y aplica un flicker suave sobre el Point Light principal usando rangos configurables de intensidad y alcance.
/// El script toma los valores iniciales del FirePointLight como referencia base y no modifica color, sombras ni tipo de luz.
/// </summary>
[DisallowMultipleComponent]
public sealed class FirePillar : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField] private ParticleSystem _fireLoopParticle;
    [SerializeField] private ParticleSystem _igniteBurstParticle;
    [SerializeField] private ParticleSystem _sparksParticle;

    [Header("Fire Material Reveal")]
    [SerializeField] private Renderer _fireRenderer;
    [SerializeField] private bool _animateAppearThreshold = true;
    [SerializeField] private string _appearThresholdProperty = "_AppearThreshold";
    [SerializeField] private float _hiddenAppearThreshold = 0f;
    [SerializeField] private float _visibleAppearThreshold = 1f;
    [SerializeField, Min(0f)] private float _fireRevealDuration = 0.25f;

    [Header("Ignition Sparks")]
    [SerializeField, Min(0)] private int _firstSparkBurstAmount = 24;
    [SerializeField, Min(0)] private int _secondSparkBurstAmount = 12;
    [SerializeField, Min(0f)] private float _secondSparkBurstDelay = 0.08f;

    [Header("Main Fire Light")]
    [SerializeField] private Light _fireLight;

    [Header("Light Flicker Bounds")]
    [SerializeField] private bool _useFlicker = true;

    [Tooltip("Intensidad mínima final entre la que oscila el FirePointLight.")]
    [SerializeField, Min(0f)] private float _minIntensity = 1.25f;

    [Tooltip("Intensidad máxima final entre la que oscila el FirePointLight.")]
    [SerializeField, Min(0f)] private float _maxIntensity = 1.75f;

    [Tooltip("Rango mínimo final entre el que oscila el FirePointLight.")]
    [SerializeField, Min(0f)] private float _minRange = 3.5f;

    [Tooltip("Rango máximo final entre el que oscila el FirePointLight.")]
    [SerializeField, Min(0f)] private float _maxRange = 4.25f;

    [SerializeField, Min(0.01f)] private float _flickerSpeed = 8f;

    [Header("State")]
    [SerializeField] private bool _startLit;
    [SerializeField] private bool _allowReplayIgnition;

    private const float IgnitionFlashIntensityMultiplier = 1.85f;
    private const float IgnitionFlashRangeMultiplier = 1.15f;

    private MaterialPropertyBlock _propertyBlock;
    private Coroutine _igniteRoutine;

    private bool _isLit;
    private float _flickerSeed;

    private float _initialLightIntensity;
    private float _initialLightRange;

    public bool IsLit => _isLit;

    public float EstimatedIgnitionDuration =>
        (_fireRevealDuration + _secondSparkBurstDelay + 0.28f);

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _flickerSeed = Random.Range(0f, 1000f);

        ResolveMissingReferences();
        CacheInitialLightValues();
        ValidateFlickerBounds();

        SetLitImmediate(_startLit);
    }

    private void Update()
    {
        if (!_isLit || !_useFlicker || _fireLight == null || _igniteRoutine != null)
            return;

        ApplyLightFlicker();
    }

    /// <summary>
    /// Enciende el pilar. Si ya está encendido, ignora la llamada salvo que Allow Replay Ignition esté activo.
    /// </summary>
    [ContextMenu("Ignite")]
    public void Ignite()
    {
        if (!isActiveAndEnabled)
            return;

        if (_isLit && !_allowReplayIgnition)
            return;

        if (_igniteRoutine != null)
            StopCoroutine(_igniteRoutine);

        _igniteRoutine = StartCoroutine(IgniteRoutine());
    }

    /// <summary>
    /// Apaga el pilar y limpia partículas visibles. Útil para resetear puzzles o testear desde el Inspector.
    /// </summary>
    [ContextMenu("Extinguish")]
    public void Extinguish()
    {
        if (_igniteRoutine != null)
            StopCoroutine(_igniteRoutine);

        _igniteRoutine = null;
        SetLitImmediate(false);
    }

    /// <summary>
    /// Copia los valores actuales del FirePointLight y arma rangos suaves de flicker alrededor de ellos.
    /// Usalo desde el menú contextual del componente si ajustaste la luz visualmente en el Inspector.
    /// </summary>
    [ContextMenu("Capture Light Values As Flicker Bounds")]
    private void CaptureLightValuesAsFlickerBounds()
    {
        ResolveMissingReferences();

        if (_fireLight == null)
            return;

        float currentIntensity = Mathf.Max(0f, _fireLight.intensity);
        float currentRange = Mathf.Max(0f, _fireLight.range);

        _minIntensity = currentIntensity * 0.85f;
        _maxIntensity = currentIntensity * 1.15f;

        _minRange = currentRange * 0.9f;
        _maxRange = currentRange * 1.1f;

        CacheInitialLightValues();
        ValidateFlickerBounds();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private IEnumerator IgniteRoutine()
    {
        _isLit = true;

        StopParticle(_fireLoopParticle, true);
        StopParticle(_igniteBurstParticle, true);
        StopParticle(_sparksParticle, true);

        SetFireVisible(false);
        SetAppearThreshold(_hiddenAppearThreshold);
        SetLightValues(0f, 0f, true);

        PlayParticle(_igniteBurstParticle, true);
        PlayParticle(_sparksParticle, true);
        EmitSparks(_firstSparkBurstAmount);

        yield return FlashLight(
            duration: 0.16f,
            targetIntensity: GetIgnitionFlashIntensity(),
            targetRange: GetIgnitionFlashRange()
        );

        if (_secondSparkBurstDelay > 0f)
            yield return new WaitForSeconds(_secondSparkBurstDelay);

        EmitSparks(_secondSparkBurstAmount);

        yield return FlashLight(
            duration: 0.12f,
            targetIntensity: GetIgnitionFlashIntensity() * 0.65f,
            targetRange: GetIgnitionFlashRange() * 0.85f
        );

        SetFireVisible(true);
        PlayParticle(_fireLoopParticle, true);
        PlayParticle(_sparksParticle, false);

        yield return RevealFireAndLight();

        SetAppearThreshold(_visibleAppearThreshold);
        SetLightValues(_initialLightIntensity, _initialLightRange, true);

        _igniteRoutine = null;
    }

    private IEnumerator RevealFireAndLight()
    {
        if (_fireRevealDuration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < _fireRevealDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / _fireRevealDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            SetAppearThreshold(Mathf.Lerp(_hiddenAppearThreshold, _visibleAppearThreshold, easedT));

            float intensity = Mathf.Lerp(0f, _initialLightIntensity, easedT);
            float range = Mathf.Lerp(0f, _initialLightRange, easedT);

            SetLightValues(intensity, range, true);

            yield return null;
        }
    }

    private IEnumerator FlashLight(float duration, float targetIntensity, float targetRange)
    {
        if (_fireLight == null || duration <= 0f)
            yield break;

        float halfDuration = duration * 0.5f;

        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / halfDuration);

            SetLightValues(
                Mathf.Lerp(0f, targetIntensity, t),
                Mathf.Lerp(0f, targetRange, t),
                true
            );

            yield return null;
        }

        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / halfDuration);

            SetLightValues(
                Mathf.Lerp(targetIntensity, 0f, t),
                Mathf.Lerp(targetRange, 0f, t),
                true
            );

            yield return null;
        }

        SetLightValues(0f, 0f, true);
    }

    private void SetLitImmediate(bool lit)
    {
        _isLit = lit;

        StopParticle(_igniteBurstParticle, true);
        SetFireVisible(lit);
        SetAppearThreshold(lit ? _visibleAppearThreshold : _hiddenAppearThreshold);

        if (lit)
        {
            PlayParticle(_fireLoopParticle, true);
            PlayParticle(_sparksParticle, true);
            SetLightValues(_initialLightIntensity, _initialLightRange, true);
            return;
        }

        StopParticle(_fireLoopParticle, true);
        StopParticle(_sparksParticle, true);
        SetLightValues(0f, 0f, false);
    }

    private void ApplyLightFlicker()
    {
        float noise = Mathf.PerlinNoise(_flickerSeed, Time.time * _flickerSpeed);
        float intensity = Mathf.Lerp(_minIntensity, _maxIntensity, noise);

        float rangeNoise = Mathf.PerlinNoise(_flickerSeed + 31.7f, Time.time * _flickerSpeed * 0.85f);
        float range = Mathf.Lerp(_minRange, _maxRange, rangeNoise);

        SetLightValues(intensity, range, true);
    }

    private void CacheInitialLightValues()
    {
        if (_fireLight == null)
        {
            _initialLightIntensity = 0f;
            _initialLightRange = 0f;
            return;
        }

        _initialLightIntensity = Mathf.Max(0f, _fireLight.intensity);
        _initialLightRange = Mathf.Max(0f, _fireLight.range);

        // Si los bounds están sin configurar, se inicializan usando los valores actuales del Point Light.
        // Si ya los configuraste manualmente, no se pisan.
        if (_minIntensity <= 0f && _maxIntensity <= 0f)
        {
            _minIntensity = _initialLightIntensity * 0.85f;
            _maxIntensity = _initialLightIntensity * 1.15f;
        }

        if (_minRange <= 0f && _maxRange <= 0f)
        {
            _minRange = _initialLightRange * 0.9f;
            _maxRange = _initialLightRange * 1.1f;
        }
    }

    private void ValidateFlickerBounds()
    {
        if (_maxIntensity < _minIntensity)
            (_minIntensity, _maxIntensity) = (_maxIntensity, _minIntensity);

        if (_maxRange < _minRange)
            (_minRange, _maxRange) = (_maxRange, _minRange);
    }

    private float GetIgnitionFlashIntensity()
    {
        float reference = Mathf.Max(_initialLightIntensity, _maxIntensity);
        return reference * IgnitionFlashIntensityMultiplier;
    }

    private float GetIgnitionFlashRange()
    {
        float reference = Mathf.Max(_initialLightRange, _maxRange);
        return reference * IgnitionFlashRangeMultiplier;
    }

    private void SetLightValues(float intensity, float range, bool enabled)
    {
        if (_fireLight == null)
            return;

        _fireLight.enabled = enabled;
        _fireLight.intensity = Mathf.Max(0f, intensity);
        _fireLight.range = Mathf.Max(0f, range);
    }

    private void SetFireVisible(bool visible)
    {
        if (_fireRenderer != null)
            _fireRenderer.enabled = visible;
    }

    private void SetAppearThreshold(float value)
    {
        if (!_animateAppearThreshold || _fireRenderer == null || string.IsNullOrWhiteSpace(_appearThresholdProperty))
            return;

        _propertyBlock ??= new MaterialPropertyBlock();

        int propertyId = Shader.PropertyToID(_appearThresholdProperty);

        _fireRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(propertyId, value);
        _fireRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void EmitSparks(int amount)
    {
        if (_sparksParticle == null || amount <= 0)
            return;

        _sparksParticle.Emit(amount);
    }

    private static void PlayParticle(ParticleSystem particle, bool clearBeforePlay)
    {
        if (particle == null)
            return;

        if (clearBeforePlay)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        particle.Play(true);
    }

    private static void StopParticle(ParticleSystem particle, bool clear)
    {
        if (particle == null)
            return;

        ParticleSystemStopBehavior stopBehavior = clear
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        particle.Stop(true, stopBehavior);
    }

    private void ResolveMissingReferences()
    {
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            string particleName = particle.name.ToLowerInvariant();

            if (_sparksParticle == null && particleName.Contains("spark"))
            {
                _sparksParticle = particle;
                continue;
            }

            if (_igniteBurstParticle == null && particleName.Contains("ignite"))
            {
                _igniteBurstParticle = particle;
                continue;
            }

            if (_fireLoopParticle == null)
                _fireLoopParticle = particle;
        }

        if (_fireRenderer == null && _fireLoopParticle != null)
            _fireRenderer = _fireLoopParticle.GetComponent<Renderer>();

        if (_fireLight != null)
            return;

        Light[] lights = GetComponentsInChildren<Light>(true);

        foreach (Light light in lights)
        {
            string lightName = light.name.ToLowerInvariant();

            if (!lightName.Contains("spark"))
            {
                _fireLight = light;
                return;
            }
        }

        if (lights.Length > 0)
            _fireLight = lights[0];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveMissingReferences();
        ValidateFlickerBounds();
    }
#endif
}