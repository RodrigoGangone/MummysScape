using Cinemachine;
using UnityEngine;
using static UIIDs;

/// <summary> 
/// Visualizador de Tiempo: Administra la representación 3D de un reloj de arena, controlando el 
/// flujo de materiales de "líquido/arena" y efectos de latido (heartbeat) sincronizados con el conteo. 
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
    
    static readonly int FillID = Shader.PropertyToID("_Fill");
    MaterialPropertyBlock _mpbTop, _mpbBot;
    ParticleSystem[] _sandFx;
    bool _isAnimating, _isCountdown, _endRaised;
    float _from, _to, _t, _dur, _fillTop = 1f;

    bool _bootstrapped; int _lastBandages = -1;

    Vector3 _baseScale; bool _hasBaseScale;
    float _beatTimer, _pulseTimer; bool _pulsing;

    private bool _paused;     
    private bool _isLocked; 

    void Awake()
    {
        _mpbTop = new MaterialPropertyBlock();
        _mpbBot = new MaterialPropertyBlock();
        CacheSandFx();

        SetLiquidsVisible(true);
        SetBaseSandActive(false);
    }
    
    void Update()
    {
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

            if (_isCountdown && !_endRaised)
            {
                _endRaised = true;

                SetBaseSandActive(false);

                GameEventManager.Instance.levelEvents.OnDeath.Raise();
            }
        }
    }
    
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
            FxPause();
        }
        else
        {
            if (_isAnimating && _isCountdown)
            {
                FxPlay();
            }
        }
    }
    
    void OnBandagesChanged(int count)
    {
        if (!_bootstrapped)
        {
            _bootstrapped = true;
            _lastBandages = count;
            SnapByBandages(count);
            return;
        }
        
        if (count < _lastBandages) bank.Play2D(Hourglass.UnWrap);
        else if (count > _lastBandages) bank.Play2D(Hourglass.Wrap);
        
        if (count == 0 && _lastBandages > 0) StartCountdown();
        else if (count > 0 && _lastBandages == 0) ResetAndFill();

        _lastBandages = count;
    }

    void OnCountDownEnded()
    {
        _animator.SetTrigger("Death");
        ExplosionPlay();

        bank.Play2D(Hourglass.Break);
        
        SetLiquidsVisible(false);
        SetBaseSandActive(false);
    }

    void StartCountdown()
    {
        _isCountdown = true;
        _endRaised = false;

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

        SetLiquidsVisible(true);
        SetBaseSandActive(false);

        bank.Play2D(Hourglass.Refill);
        
        BeginAnim(_fillTop, 1f, _resetDuration);
        HeartbeatStop();
        FxReset();
    }

    void SnapByBandages(int bandages)
    {
        _isCountdown = false;
        _isAnimating = false;

        SetLiquidsVisible(true);
        SetBaseSandActive(false);

        HeartbeatStop();
        FxReset();
        ApplyFill(bandages > 0 ? 1f : 0f);
        _endRaised = false;
    }

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

    void HeartbeatTick(float countdownProgress01)
    {
        if (_heartbeatTarget == null) return;

        if (!_hasBaseScale) { _baseScale = _heartbeatTarget.localScale; _hasBaseScale = true; }
        float interval = Mathf.Lerp(_beatIntervalStart, _beatIntervalEnd, countdownProgress01);

        _beatTimer += Time.deltaTime;
        
        if (_beatTimer >= interval) 
        { 
            _beatTimer = 0f; 
            _pulsing = true; 
            _pulseTimer = 0f; 
            
            bank.Play2D(Hourglass.Beat);
            
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

    void SetBaseSandActive(bool active)
    {
        if (_baseSand && _baseSand.activeSelf != active) _baseSand.SetActive(active);
    }

    void SetLiquidsVisible(bool visible)
    {
        if (_topLiquidRenderer)    _topLiquidRenderer.enabled    = visible;
        if (_bottomLiquidRenderer) _bottomLiquidRenderer.enabled = visible;
    }
    
    void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);
        
        GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Register<int>(OnBandagesChanged);
        GameEventManager.Instance.levelEvents.OnDeath.Register(OnCountDownEnded);
    }

    void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);

        GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Unregister<int>(OnBandagesChanged);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(OnCountDownEnded);
        HeartbeatStop();
        FxReset();
    }
}