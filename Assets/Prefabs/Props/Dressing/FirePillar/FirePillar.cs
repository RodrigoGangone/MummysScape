using System.Collections;
using UnityEngine;

/// <summary>
/// Controla un pilar de fuego individual: mantiene el fuego apagado o encendido, reproduce el encendido inicial
/// con destellos/chispas y sincroniza la iluminación principal con un flicker suave para simular fuego real.
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
    [SerializeField] private Color _fireColor = new(1f, 0.45f, 0.08f, 1f);
    [SerializeField, Min(0f)] private float _idleIntensity = 2.2f;
    [SerializeField, Min(0f)] private float _idleRange = 2.4f;
    [SerializeField, Min(0f)] private float _flashIntensity = 7f;
    [SerializeField, Min(0f)] private float _flashRange = 4f;
    [SerializeField, Min(0f)] private float _flashDuration = 0.16f;

    [Header("Flicker")]
    [SerializeField] private bool _useFlicker = true;
    [SerializeField, Min(0f)] private float _intensityFlicker = 0.35f;
    [SerializeField, Min(0f)] private float _rangeFlicker = 0.2f;
    [SerializeField, Min(0.01f)] private float _flickerSpeed = 8f;

    [Header("State")]
    [SerializeField] private bool _startLit;
    [SerializeField] private bool _allowReplayIgnition;

    private MaterialPropertyBlock _propertyBlock;
    private Coroutine _igniteRoutine;
    private bool _isLit;
    private float _flickerSeed;

    public bool IsLit => _isLit;

    public float EstimatedIgnitionDuration =>
        (_flashDuration * 1.75f) + _secondSparkBurstDelay + Mathf.Max(0f, _fireRevealDuration);

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _flickerSeed = Random.Range(0f, 1000f);

        ResolveMissingReferences();
        ConfigureLightBaseValues();
        SetLitImmediate(_startLit);
    }

    private void Update()
    {
        if (!_isLit || !_useFlicker || _fireLight == null)
            return;

        float noise = Mathf.PerlinNoise(_flickerSeed, Time.time * _flickerSpeed);
        float centeredNoise = (noise * 2f) - 1f;

        _fireLight.intensity = Mathf.Max(0f, _idleIntensity + centeredNoise * _intensityFlicker);
        _fireLight.range = Mathf.Max(0f, _idleRange + centeredNoise * _rangeFlicker);
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

        yield return FlashLight(_flashDuration, _flashIntensity, _flashRange);

        if (_secondSparkBurstDelay > 0f)
            yield return new WaitForSeconds(_secondSparkBurstDelay);

        EmitSparks(_secondSparkBurstAmount);
        yield return FlashLight(_flashDuration * 0.75f, _flashIntensity * 0.65f, _flashRange * 0.85f);

        SetFireVisible(true);
        PlayParticle(_fireLoopParticle, true);
        PlayParticle(_sparksParticle, false);

        yield return RevealFireAndLight();

        SetAppearThreshold(_visibleAppearThreshold);
        SetLightValues(_idleIntensity, _idleRange, true);
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
            SetLightValues(Mathf.Lerp(0f, _idleIntensity, easedT), Mathf.Lerp(0f, _idleRange, easedT), true);

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
            SetLightValues(Mathf.Lerp(0f, targetIntensity, t), Mathf.Lerp(0f, targetRange, t), true);
            yield return null;
        }

        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / halfDuration);
            SetLightValues(Mathf.Lerp(targetIntensity, 0f, t), Mathf.Lerp(targetRange, 0f, t), true);
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
            SetLightValues(_idleIntensity, _idleRange, true);
        }
        else
        {
            StopParticle(_fireLoopParticle, true);
            StopParticle(_sparksParticle, true);
            SetLightValues(0f, 0f, false);
        }
    }

    private void ConfigureLightBaseValues()
    {
        if (_fireLight == null)
            return;

        _fireLight.color = _fireColor;
        _fireLight.shadows = LightShadows.None;
    }

    private void SetLightValues(float intensity, float range, bool enabled)
    {
        if (_fireLight == null)
            return;

        _fireLight.enabled = enabled;
        _fireLight.color = _fireColor;
        _fireLight.intensity = intensity;
        _fireLight.range = range;
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
            if (!light.name.ToLowerInvariant().Contains("spark"))
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
        if (!Application.isPlaying)
            ResolveMissingReferences();
    }
#endif
}
