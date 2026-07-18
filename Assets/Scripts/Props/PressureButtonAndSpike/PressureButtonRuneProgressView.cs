using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sincroniza la propiedad Progress del material de las runas con el temporizador del botón.
/// Durante la cuenta regresiva aplica el progreso de 0 a 1 y, cuando el temporizador se reinicia
/// o finaliza la retención, interpola rápidamente el valor visual actual hasta 0.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DecalProjector))]
public sealed class PressureButtonRuneProgressView : MonoBehaviour
{
    private const string DefaultProgressProperty = "_Progress";
    private const float MinReturnDuration = 0.01f;

    [Header("References")]
    [SerializeField] private PressureButtonHoldTimer _holdTimer;
    [SerializeField] private DecalProjector _decalProjector;

    [Header("Shader")]
    [SerializeField] private string _progressProperty = DefaultProgressProperty;

    [Header("Return To Visible")]
    [Tooltip("Tiempo que tarda Progress en volver desde su valor actual hasta 0.")]
    [SerializeField, Min(MinReturnDuration)]
    private float _returnDuration = 0.1f;

    [Tooltip("Curva utilizada cuando las runas vuelven a hacerse visibles.")]
    [SerializeField]
    private AnimationCurve _returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Material _sharedMaterial;
    private Material _runtimeMaterial;

    private int _progressPropertyId;
    private bool _hasProgressProperty;

    private float _displayedProgress;
    private float _returnStartProgress;
    private float _returnElapsed;
    private bool _isReturningToVisible;
    private bool _wasTimerRunning;

    private void Awake()
    {
        ResolveReferences();

        _progressPropertyId = Shader.PropertyToID(
            string.IsNullOrWhiteSpace(_progressProperty)
                ? DefaultProgressProperty
                : _progressProperty);

        CreateRuntimeMaterial();
        ApplyProgressImmediate(0f);
    }

    private void OnEnable()
    {
        if (_holdTimer == null)
        {
            Debug.LogWarning(
                $"{nameof(PressureButtonRuneProgressView)} requiere una referencia a " +
                $"{nameof(PressureButtonHoldTimer)}.",
                this);
            return;
        }

        _holdTimer.ProgressChanged += HandleTimerProgressChanged;
        _wasTimerRunning = _holdTimer.IsRunning;
        HandleTimerProgressChanged(_holdTimer.Progress);
    }

    private void Update()
    {
        if (_holdTimer != null && _holdTimer.IsRunning && !_wasTimerRunning)
        {
            // Una nueva cuenta regresiva siempre comienza visualmente desde Progress = 0.
            StopReturnAnimation();
            ApplyProgressImmediate(_holdTimer.Progress);
        }

        _wasTimerRunning = _holdTimer != null && _holdTimer.IsRunning;

        if (!_isReturningToVisible)
        {
            return;
        }

        _returnElapsed += Time.deltaTime;

        float normalizedTime = Mathf.Clamp01(_returnElapsed / _returnDuration);
        float curvedTime = _returnCurve != null
            ? _returnCurve.Evaluate(normalizedTime)
            : normalizedTime;

        ApplyProgressImmediate(Mathf.Lerp(_returnStartProgress, 0f, curvedTime));

        if (normalizedTime < 1f)
        {
            return;
        }

        StopReturnAnimation();
        ApplyProgressImmediate(0f);
    }

    private void OnDisable()
    {
        if (_holdTimer != null)
        {
            _holdTimer.ProgressChanged -= HandleTimerProgressChanged;
        }

        _wasTimerRunning = false;
        StopReturnAnimation();
        ApplyProgressImmediate(0f);
    }

    private void HandleTimerProgressChanged(float timerProgress)
    {
        float targetProgress = Mathf.Clamp01(timerProgress);

        if (_holdTimer != null && _holdTimer.IsRunning)
        {
            // El timer ya entrega un valor normalizado que avanza de 0 a 1
            // mientras disminuye el tiempo restante.
            StopReturnAnimation();
            ApplyProgressImmediate(targetProgress);
            return;
        }

        if (targetProgress >= _displayedProgress)
        {
            // Al finalizar el countdown, el timer publica Progress = 1 antes
            // de cambiar el estado efectivo. Se aplica inmediatamente.
            StopReturnAnimation();
            ApplyProgressImmediate(targetProgress);
            return;
        }

        // Al cancelar o resetear el countdown, Progress baja hacia 0.
        // La vista realiza ese retorno suavemente en _returnDuration segundos.
        BeginReturnToVisible();
    }

    private void BeginReturnToVisible()
    {
        _returnStartProgress = _displayedProgress;
        _returnElapsed = 0f;

        if (_returnDuration <= Mathf.Epsilon || Mathf.Approximately(_returnStartProgress, 0f))
        {
            StopReturnAnimation();
            ApplyProgressImmediate(0f);
            return;
        }

        _isReturningToVisible = true;
    }

    private void StopReturnAnimation()
    {
        _isReturningToVisible = false;
        _returnElapsed = 0f;
    }

    private void ApplyProgressImmediate(float progress)
    {
        _displayedProgress = Mathf.Clamp01(progress);

        if (_runtimeMaterial == null || !_hasProgressProperty)
        {
            return;
        }

        _runtimeMaterial.SetFloat(_progressPropertyId, _displayedProgress);
    }

    private void CreateRuntimeMaterial()
    {
        if (_decalProjector == null || _decalProjector.material == null)
        {
            Debug.LogWarning(
                $"{nameof(PressureButtonRuneProgressView)} no tiene un material de decal asignado.",
                this);
            return;
        }

        _sharedMaterial = _decalProjector.material;
        _runtimeMaterial = new Material(_sharedMaterial)
        {
            name = $"{_sharedMaterial.name} ({name} Runtime)"
        };

        _decalProjector.material = _runtimeMaterial;
        _hasProgressProperty = _runtimeMaterial.HasProperty(_progressPropertyId);

        if (_hasProgressProperty)
        {
            return;
        }

        Debug.LogWarning(
            $"El material '{_runtimeMaterial.name}' no contiene la propiedad '{_progressProperty}'.",
            this);
    }

    private void ResolveReferences()
    {
        if (_decalProjector == null)
        {
            _decalProjector = GetComponent<DecalProjector>();
        }

        if (_holdTimer == null)
        {
            _holdTimer = GetComponentInParent<PressureButtonHoldTimer>();
        }
    }

    private void OnDestroy()
    {
        if (_decalProjector != null && _decalProjector.material == _runtimeMaterial)
        {
            _decalProjector.material = _sharedMaterial;
        }

        if (_runtimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(_runtimeMaterial);
        }
    }

    private void OnValidate()
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(_progressProperty))
        {
            _progressProperty = DefaultProgressProperty;
        }

        _returnDuration = Mathf.Max(MinReturnDuration, _returnDuration);

        if (_returnCurve == null || _returnCurve.length == 0)
        {
            _returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }
}