using UnityEngine;

/// <summary>
/// HourglassManager
/// Administra el reloj de arena (TOP/BOTTOM comparten _Fill; cada material lo interpreta distinto).
/// Se suscribe a OnBandagesCountChanged para iniciar countdown (0 vendas) o resetear (>0 vendas).
/// Incluye atajos de debug (J/K) y un efecto de "latidos" que solo corre durante el countdown,
/// acelerando a medida que el tiempo restante se agota.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class HourglassManager : MonoBehaviour
{
    [Header("Renderers (asignar materiales correctos)")]
    [SerializeField] private MeshRenderer _topLiquidRenderer;    
    [SerializeField] private MeshRenderer _bottomLiquidRenderer; 

    [Header("Duraciones (segundos)")]
    [Min(0f)] [SerializeField] private float _countdownDuration = 30f;   
    [Min(0f)] [SerializeField] private float _resetDuration     = 0.75f; 

    [Header("Curvas")]
    [SerializeField] private AnimationCurve _countdownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve _resetCurve     = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug / Test (opcional)")]
    [SerializeField] private bool _enableDebugKeys = true;
    [SerializeField] private KeyCode _keyNoBandages   = KeyCode.J; // Raise(0)
    [SerializeField] private KeyCode _keySomeBandages = KeyCode.K; // Raise(1)

    [Header("Heartbeat (solo durante countdown)")]
    [SerializeField] private Transform _heartbeatTarget;     // GO a escalar
    [SerializeField, Min(0.01f)] private float _pulseScale = 0.25f;
    [SerializeField, Min(0.01f)] private float _pulseDuration = 0.5f;
    [Tooltip("Intervalo inicial (lento) → final (rápido) a lo largo del countdown")]
    [SerializeField, Min(0.01f)] private float _beatIntervalStart = 1f;
    [SerializeField, Min(0.01f)] private float _beatIntervalEnd   = 0.5f;
    [SerializeField] private AnimationCurve _pulseCurve =
        new(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.25f, 1f, 0f, 0f),
            new Keyframe(0.5f, 0.5f, 0f, 0f),
            new Keyframe(0.75f,  0.75f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f)
        );
    
    [Header("FX Arena (solo durante countdown)")]
    [Tooltip("GameObject que contiene los ParticleSystem de arena cayendo.")]
    [SerializeField] private GameObject _sandFxRoot;
    [Tooltip("Si está activo, habilita/deshabilita el GameObject raíz de FX al iniciar/terminar el countdown.")]
    [SerializeField] private bool _toggleFxGameObject = true;
    [Tooltip("Si está activo, limpia las partículas al detenerlas.")]
    [SerializeField] private bool _clearFxOnStop = true;
    // FX cache
    private ParticleSystem[] _sandFxSystems;

    private static readonly int FillID = Shader.PropertyToID("_Fill");

    private MaterialPropertyBlock _topMPB;
    private MaterialPropertyBlock _botMPB;

    // "Cuánto está lleno el TOP" [0..1]; el BOTTOM interpreta a la inversa.
    private float _fillTop = 1f;

    // Animación de arena
    private bool _animating;
    private bool _isCountdown; // true si la animación en curso es countdown (no reset)
    private float _from, _to, _elapsed, _duration;
    private AnimationCurve _curve;

    // Evento
    private bool _bootstrapped;
    private int  _lastBandages = -1;

    // Heartbeat runtime
    private Vector3 _baseScale;
    private bool _hasBaseScale;
    private float _beatTimer;
    private bool _pulsing;
    private float _pulseTimer;

    private void Awake()
    {
        _topMPB = new MaterialPropertyBlock();
        _botMPB = new MaterialPropertyBlock();

        var evt = GameEventManager.Instance.playerEvents.OnBandagesCountChanged;
        if (evt != null) evt.Register<int>(OnBandagesChanged);
        // Esperamos el primer Raise del sistema de jugador para definir el estado inicial.
        
        // Cachea los ParticleSystems si hay root asignado
        CacheSandFx();
    }

    private void OnDisable()
    {
        var evt = GameEventManager.Instance.playerEvents.OnBandagesCountChanged;
        if (evt != null) evt.Unregister<int>(OnBandagesChanged);
        StopHeartbeat(); // por seguridad
    }

    private void Update()
    {
        // Debug keys
        if (_enableDebugKeys)
        {
            if (Input.GetKeyDown(_keyNoBandages))   SafeRaiseBandages(0);
            if (Input.GetKeyDown(_keySomeBandages)) SafeRaiseBandages(1);
        }

        // Animación de arena
        if (_animating)
        {
            _elapsed += Time.deltaTime;
            float progress = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
            float shaped   = (_curve != null) ? _curve.Evaluate(progress) : progress;

            ApplyFill(Mathf.LerpUnclamped(_from, _to, shaped));

            // Heartbeat: solo cuando es countdown
            if (_isCountdown) UpdateHeartbeat(progress);

            if (progress >= 1f - Mathf.Epsilon)
            {
                _animating = false;
                ApplyFill(_to);
                StopHeartbeat();
            }
        }
    }

    // -------------------- Eventos --------------------
    private static void SafeRaiseBandages(int value)
    {
        var evt = GameEventManager.Instance.playerEvents.OnBandagesCountChanged;
        if (evt != null) evt.Raise(value);
    }

    private void OnBandagesChanged(int count)
    {
        if (!_bootstrapped)
        {
            _bootstrapped = true;
            _lastBandages = count;
            SnapByBandageCount(count);
            return;
        }

        if (count == 0 && _lastBandages > 0)         // perdió todas
            StartCountdown();
        else if (count > 0 && _lastBandages == 0)    // volvió a tener
            ResetAndFill();

        _lastBandages = count;
    }

    // -------------------- API pública --------------------
    /// <summary>Vacía el slot superior (TOP 1→0) y llena el inferior.</summary>
    public void StartCountdown()
    {
        _isCountdown = true;
        BeginAnim(_fillTop, 0f, _countdownDuration, _countdownCurve);
        ResetHeartbeatState(); // arranca el latido limpio
        StartSandFx();
    }

    /// <summary>Llena el slot superior (TOP 0→1) y vacía el inferior.</summary>
    public void ResetAndFill()
    {
        _isCountdown = false;
        BeginAnim(_fillTop, 1f, _resetDuration, _resetCurve);
        StopHeartbeat();
        StopSandFx();
    }

    /// <summary>Fija el estado inmediato según # de vendas (sin animación).</summary>
    public void SnapByBandageCount(int bandages)
    {
        _isCountdown = false;
        _animating = false;
        StopHeartbeat();
        StopSandFx();
        ApplyFill(bandages > 0 ? 1f : 0f);
    }

    // -------------------- Internos: animación arena --------------------
    private void BeginAnim(float from, float to, float duration, AnimationCurve curve)
    {
        _from = Mathf.Clamp01(from);
        _to = Mathf.Clamp01(to);
        _duration = Mathf.Max(0f, duration);
        _curve = curve ?? AnimationCurve.Linear(0, 0, 1, 1);
        _elapsed = 0f;
        _animating = _duration > 0f;

        if (!_animating) ApplyFill(_to);
    }

    private void ApplyFill(float topFill01)
    {
        _fillTop = Mathf.Clamp01(topFill01);

        if (_topLiquidRenderer)
        {
            _topLiquidRenderer.GetPropertyBlock(_topMPB);
            _topMPB.SetFloat(FillID, _fillTop);
            _topLiquidRenderer.SetPropertyBlock(_topMPB);
        }
        if (_bottomLiquidRenderer)
        {
            _bottomLiquidRenderer.GetPropertyBlock(_botMPB);
            _botMPB.SetFloat(FillID, _fillTop);
            _bottomLiquidRenderer.SetPropertyBlock(_botMPB);
        }
    }

    // -------------------- Internos: heartbeat --------------------
    private void UpdateHeartbeat(float countdownProgress01)
    {
        if (_heartbeatTarget == null) return;

        if (!_hasBaseScale)
        {
            _baseScale = _heartbeatTarget.localScale;
            _hasBaseScale = true;
        }

        // Intervalo actual según progreso (0 = inicio lento, 1 = final rápido)
        float interval = Mathf.Lerp(_beatIntervalStart, _beatIntervalEnd, countdownProgress01);

        // Disparo de nuevo latido
        _beatTimer += Time.deltaTime;
        if (_beatTimer >= interval)
        {
            _beatTimer = 0f;
            _pulsing = true;
            _pulseTimer = 0f;
        }

        // Si estamos en pulso, aplicar envelope
        if (_pulsing)
        {
            _pulseTimer += Time.deltaTime;
            float n = Mathf.Clamp01(_pulseTimer / _pulseDuration);
            float env = (_pulseCurve != null) ? _pulseCurve.Evaluate(n) : 1f; // pico
            float scaleFactor = 1f + (_pulseScale * env);

            _heartbeatTarget.localScale = _baseScale * scaleFactor;

            if (n >= 1f)
            {
                _pulsing = false;
                _heartbeatTarget.localScale = _baseScale; // volver a base tras el golpe
            }
        }
    }

    private void ResetHeartbeatState()
    {
        if (_heartbeatTarget == null) return;
        if (!_hasBaseScale)
        {
            _baseScale = _heartbeatTarget.localScale;
            _hasBaseScale = true;
        }
        _beatTimer = 0f;
        _pulsing = false;
        _pulseTimer = 0f;
        _heartbeatTarget.localScale = _baseScale;
    }

    private void StopHeartbeat()
    {
        if (_heartbeatTarget == null) return;
        if (_hasBaseScale) _heartbeatTarget.localScale = _baseScale;
        _pulsing = false;
        _beatTimer = 0f;
        _pulseTimer = 0f;
    }
    
    // -------------------- Internos: FX arena --------------------
    private void CacheSandFx()
    {
        if (_sandFxRoot == null) { _sandFxSystems = null; return; }
        _sandFxSystems = _sandFxRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    private void StartSandFx()
    {
        if (_sandFxRoot == null) return;

        if (_sandFxSystems == null || _sandFxSystems.Length == 0)
            CacheSandFx();

        if (_toggleFxGameObject && !_sandFxRoot.activeSelf)
            _sandFxRoot.SetActive(true);

        if (_sandFxSystems != null)
        {
            foreach (var ps in _sandFxSystems)
            {
                if (ps == null) continue;
                ps.Play(true);
            }
        }
    }

    private void StopSandFx()
    {
        if (_sandFxRoot == null) return;

        if (_sandFxSystems == null || _sandFxSystems.Length == 0)
            CacheSandFx();

        if (_sandFxSystems != null)
        {
            foreach (var ps in _sandFxSystems)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (_clearFxOnStop) ps.Clear(true);
            }
        }

        if (_toggleFxGameObject && _sandFxRoot.activeSelf)
            _sandFxRoot.SetActive(false);
    }
}