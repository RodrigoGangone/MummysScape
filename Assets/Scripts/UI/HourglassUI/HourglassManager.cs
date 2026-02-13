using Cinemachine;
using UnityEngine;

/// <summary>
/// HourglassManager
/// - Administra el Fill de TOP/BOTTOM.
/// - Se detiene completamente si hay PAUSA o LOCK.
/// </summary>
[DisallowMultipleComponent]
public sealed class HourglassManager : MonoBehaviour, IPausable
{
    [Header("Renderers")]
    [SerializeField] MeshRenderer _topLiquidRenderer;
    [SerializeField] MeshRenderer _bottomLiquidRenderer;

    [Header("BaseLiquid")]
    [SerializeField] GameObject _baseSand;

    [Header("Tiempos (s)")]
    [Min(0f)][SerializeField] float _countdownDuration = 30f;
    [Min(0f)][SerializeField] float _resetDuration     = 0.75f;

    [Header("Heartbeat (solo countdown)")]
    [SerializeField] Transform _heartbeatTarget;
    [Min(0.01f)][SerializeField] float _pulseScale = 0.25f;
    [Min(0.01f)][SerializeField] float _pulseDuration = 0.5f;
    [Tooltip("Inicio lento → final rápido a lo largo del countdown")]
    [Min(0.01f)][SerializeField] float _beatIntervalStart = 1f;
    [Min(0.01f)][SerializeField] float _beatIntervalEnd   = 0.5f;
    [SerializeField] AnimationCurve _pulseCurve =
        new(new Keyframe(0f,0f), new Keyframe(0.25f,1f), new Keyframe(0.5f,0.5f), new Keyframe(0.75f,0.75f), new Keyframe(1f,0f));

    [Header("FX Arena (solo countdown)")]
    [Tooltip("Raíz con los ParticleSystem de arena cayendo (GO activo en escena).")]
    [SerializeField] GameObject _sandFxRoot;

    [Header("FX Final")]
    [Tooltip("FXHourglassSandExplosion (one-shot, no loopeable).")]
    [SerializeField] ParticleSystem _endExplosionFx;

    [Header("Animaciones")]
    [SerializeField] Animator _animator;

    [SerializeField] private FxBank bank;
    
    [Tooltip("Referencia al componente que genera el temblor.")] [SerializeField]
    private CinemachineImpulseSource _impulseSource; 
    [SerializeField] float _shakeForce = 0.2f; 
    
    // --- Internos ---
    static readonly int FillID = Shader.PropertyToID("_Fill");
    MaterialPropertyBlock _mpbTop, _mpbBot;
    ParticleSystem[] _sandFx;
    bool _isAnimating, _isCountdown, _endRaised;
    float _from, _to, _t, _dur, _fillTop = 1f;

    bool _bootstrapped; int _lastBandages = -1;

    // heartbeat runtime
    Vector3 _baseScale; bool _hasBaseScale;
    float _beatTimer, _pulseTimer; bool _pulsing;

    // ⏸️ Estados de Pausa
    private bool _paused;     // Menú
    private bool _isLocked;   // Cinemática

    // ---------------- Ciclo de vida ----------------
    void Awake()
    {
        _mpbTop = new MaterialPropertyBlock();
        _mpbBot = new MaterialPropertyBlock();
        CacheSandFx();

        // Estado visual por defecto: líquidos visibles, baseSand oculta
        SetLiquidsVisible(true);
        SetBaseSandActive(false);
    }

    void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        // 🔹 REGISTRAMOS EL LOCKED
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
        
        GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Register<int>(OnBandagesChanged);
        GameEventManager.Instance.levelEvents.OnDeath.Register(OnCountDownEnded);
    }

    void OnDisable()
    {
        if (GameEventManager.Instance == null) return;

        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        // 🔹 DES-REGISTRAMOS EL LOCKED
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);

        GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Unregister<int>(OnBandagesChanged);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(OnCountDownEnded);
        HeartbeatStop();
        FxReset();
    }

    void Update()
    {
        // DEBUG: teclas directas (TODO: quitar cuando no se usen)
        if (Input.GetKeyDown(KeyCode.J)) GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Raise(0);
        if (Input.GetKeyDown(KeyCode.K)) GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Raise(1);

        // 🔹 AQUÍ ESTÁ LA MAGIA: Si está pausado O bloqueado, no procesamos tiempo.
        // Al no procesar tiempo, '_t' no aumenta, por lo tanto no llegamos al final de la animación (Muerte).
        if (!_isAnimating || _paused || _isLocked) return;

        _t += Time.deltaTime;
        float p = _dur <= 0f ? 1f : Mathf.Clamp01(_t / _dur);

        ApplyFill(Mathf.LerpUnclamped(_from, _to, p));
        if (_isCountdown) HeartbeatTick(p);

        if (p >= 1f - Mathf.Epsilon)
        {
            _isAnimating = false;
            ApplyFill(_to);
            HeartbeatStop();
            FxReset();

            // Se terminó la animación actual
            if (_isCountdown && !_endRaised)
            {
                _endRaised = true;

                // Como ya no estamos en countdown, ocultamos baseSand aquí también
                SetBaseSandActive(false);

                GameEventManager.Instance.levelEvents.OnDeath.Raise();
            }
        }
    }

    // ---------------- Eventos de Pausa / Lock ----------------

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;
        UpdatePauseState();
    }

    public void OnLockChanged(bool locked)
    {
        _isLocked = locked;
        UpdatePauseState();
    }

    private void UpdatePauseState()
    {
        bool isFrozen = _paused || _isLocked;

        if (isFrozen)
        {
            // Congelar efectos visuales de arena
            FxPause();
        }
        else
        {
            // Reanudar SOLO si estábamos en countdown y animando
            if (_isAnimating && _isCountdown)
            {
                FxPlay();
            }
        }
    }

    // ---------------- Lógica General ----------------

    void OnBandagesChanged(int count)
    {
        if (!_bootstrapped)
        {
            _bootstrapped = true;
            _lastBandages = count;
            SnapByBandages(count);
            return;
        }
        
        // --- SONIDOS DE VENDAS ---
        if (count < _lastBandages) bank.Play2D("UnWrap");
        else if (count > _lastBandages) bank.Play2D("Wrap");
        
        if (count == 0 && _lastBandages > 0) StartCountdown();
        else if (count > 0 && _lastBandages == 0) ResetAndFill();

        _lastBandages = count;
    }

    void OnCountDownEnded()
    {
        _animator.SetTrigger("Death");
        ExplosionPlay();

        bank.Play2D("Break");
        
        // Al finalizar: ocultar líquidos y baseSand
        SetLiquidsVisible(false);
        SetBaseSandActive(false);
    }

    // ---------------- Estados ----------------
    void StartCountdown()
    {
        _isCountdown = true;
        _endRaised = false;

        // Visuales para countdown
        SetLiquidsVisible(true);
        SetBaseSandActive(true);

        BeginAnim(_fillTop, 0f, _countdownDuration);
        HeartbeatReset();
        ExplosionReset();
        FxPlay();
    }

    void ResetAndFill()
    {
        _isCountdown = false;

        // Visuales fuera de countdown: líquidos visibles, baseSand oculta
        SetLiquidsVisible(true);
        SetBaseSandActive(false);

        bank.Play2D("Refill");
        
        BeginAnim(_fillTop, 1f, _resetDuration);
        HeartbeatStop();
        FxReset();
    }

    void SnapByBandages(int bandages)
    {
        _isCountdown = false;
        _isAnimating = false;

        // Visuales fuera de countdown en snap inicial
        SetLiquidsVisible(true);
        SetBaseSandActive(false);

        HeartbeatStop();
        FxReset();
        ApplyFill(bandages > 0 ? 1f : 0f);
        _endRaised = false;
    }

    // ---------------- Arena (anim Fill) ----------------
    void BeginAnim(float from, float to, float duration)
    {
        _from = Mathf.Clamp01(from);
        _to   = Mathf.Clamp01(to);
        _dur  = Mathf.Max(0f, duration);
        _t    = 0f;
        _isAnimating = _dur > 0f;
        if (!_isAnimating) ApplyFill(_to);
    }

    void ApplyFill(float topFill01)
    {
        _fillTop = Mathf.Clamp01(topFill01);
        if (_topLiquidRenderer)
        {
            _topLiquidRenderer.GetPropertyBlock(_mpbTop);
            _mpbTop.SetFloat(FillID, _fillTop);
            _topLiquidRenderer.SetPropertyBlock(_mpbTop);
        }
        if (_bottomLiquidRenderer)
        {
            _bottomLiquidRenderer.GetPropertyBlock(_mpbBot);
            _mpbBot.SetFloat(FillID, _fillTop);
            _bottomLiquidRenderer.SetPropertyBlock(_mpbBot);
        }
    }

    // ---------------- Heartbeat ----------------
    void HeartbeatTick(float countdownProgress01)
    {
        if (_heartbeatTarget == null) return;

        if (!_hasBaseScale) { _baseScale = _heartbeatTarget.localScale; _hasBaseScale = true; }
        float interval = Mathf.Lerp(_beatIntervalStart, _beatIntervalEnd, countdownProgress01);

        _beatTimer += Time.deltaTime;
        
        // AQUÍ ES EL MOMENTO DEL LATIDO (Beat Start)
        if (_beatTimer >= interval) 
        { 
            _beatTimer = 0f; 
            _pulsing = true; 
            _pulseTimer = 0f; 
            
            bank.Play2D("Beat");
            
            if (_impulseSource != null)
            {
                _impulseSource.GenerateImpulse(_shakeForce);
            }
        }

        if (_pulsing)
        {
            _pulseTimer += Time.deltaTime;
            float n = Mathf.Clamp01(_pulseTimer / _pulseDuration);
            float env = _pulseCurve != null ? _pulseCurve.Evaluate(n) : 1f;

            _heartbeatTarget.localScale = _baseScale * (1f + _pulseScale * env);

            if (n >= 1f) { _pulsing = false; _heartbeatTarget.localScale = _baseScale; }
        }
    }

    void HeartbeatReset()
    {
        if (_heartbeatTarget == null) return;
        if (!_hasBaseScale) { _baseScale = _heartbeatTarget.localScale; _hasBaseScale = true; }
        _beatTimer = 0f; _pulseTimer = 0f; _pulsing = false;
        _heartbeatTarget.localScale = _baseScale;
    }

    void HeartbeatStop()
    {
        if (_heartbeatTarget == null) return;
        if (_hasBaseScale) _heartbeatTarget.localScale = _baseScale;
        _beatTimer = 0f; _pulseTimer = 0f; _pulsing = false;
    }

    // ---------------- FX Arena: Play / Pause / Reset ----------------
    void CacheSandFx()
    {
        _sandFx = (_sandFxRoot == null) ? null : _sandFxRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    void FxPlay()
    {
        if (_sandFxRoot == null) return;
        if (_sandFx == null || _sandFx.Length == 0) CacheSandFx();
        if (_sandFx == null) return;

        foreach (var ps in _sandFx) if (ps) ps.Play(true);
    }

    void FxPause()
    {
        if (_sandFx == null) return;
        foreach (var ps in _sandFx) if (ps) ps.Pause(true);
    }

    void FxReset()
    {
        if (_sandFx == null) return;
        foreach (var ps in _sandFx) if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ---------------- FX Final (Explosión) ----------------
    void ExplosionPlay()
    {
        if (_endExplosionFx == null) return;
        _endExplosionFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _endExplosionFx.Play(true);
    }

    void ExplosionReset()
    {
        if (_endExplosionFx == null) return;
        _endExplosionFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ---------------- Helpers visuales ----------------
    void SetBaseSandActive(bool active)
    {
        if (_baseSand && _baseSand.activeSelf != active) _baseSand.SetActive(active);
    }

    void SetLiquidsVisible(bool visible)
    {
        if (_topLiquidRenderer)    _topLiquidRenderer.enabled    = visible;
        if (_bottomLiquidRenderer) _bottomLiquidRenderer.enabled = visible;
    }
}