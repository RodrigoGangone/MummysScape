using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using static PlayerEnum;

public sealed class PlayerView : MonoBehaviour, IPausable
{
    [Header("Anim & FX")]
    [SerializeField] private Animator _anim;
    
    [SerializeField] private FxBank bankNormal;
    [SerializeField] private FxBank bankSmall;
    [SerializeField] private FxBank bankHead;

    [SerializeField] private RuntimeAnimatorController _controllerNormal;
    [SerializeField] private RuntimeAnimatorController _controllerSmall;
    [SerializeField] private RuntimeAnimatorController _controllerHead;
    
    [SerializeField] private Avatar _avatarNormal;
    [SerializeField] private Avatar _avatarSmall;
    [SerializeField] private Avatar _avatarHead;

    [SerializeField] public ParticleSystem _shootFX;
    [SerializeField] public ParticleSystem _smashFX;
    [SerializeField] public ParticleSystem _dropFX;

    [Header("Shoot Visual")]
    [SerializeField] private GameObject _decal;
    [SerializeField] private DecalProjector _rangeIndicator;
    [SerializeField] private LineRenderer _arcRenderer;
    [ColorUsage(true, true), SerializeField] private Color _aimAllowed;
    [ColorUsage(true, true), SerializeField] private Color _aimNotAllowed;    
    [Header("UI (opcional)")]
    [SerializeField] private Image _headTimerFill;
    [SerializeField] private Sprite _spriteHead;
    [SerializeField] private Sprite _spriteNormalOrSmall;

    // --- NUEVO SISTEMA CENTRALIZADO DE VENDAS ---
    [Header("Bandage Visuals System")]
    [SerializeField] private LineRenderer _bandageLine;
    
    [Header("Hand Anchors (Asignar transforms de las manos)")]
    [SerializeField] private Transform _handAnchorNormal;
    [SerializeField] private Transform _handAnchorSmall;
    [SerializeField] private Transform _handAnchorHead;

    // Estado interno del Bandage
    private Transform _currentHandAnchor; // Se actualiza al cambiar de tamaño
    private Transform _bandageTarget;     // El objeto al que nos pegamos
    private Vector3 _bandageTargetLocalOffset; // El punto exacto del golpe en local
    private bool _isBandageActive;
    
    // Shader Logic
    private Coroutine _drawCoroutine;
    private Material _bandageMatInst; 
    private int _thresholdPropID;
    private const string THRESHOLD_NAME = "_rightThreshold";
    private const float MAT_START_VAL = 1.5f;
    private const float MAT_END_VAL = 0f;

    private PlayerSize _currentSize = PlayerSize.Normal; // Tamaño por defecto
    
    public GameObject Decal => _decal;
    public DecalProjector RangeIndicator => _rangeIndicator;
    public LineRenderer ArcRenderer => _arcRenderer;
    public Animator Animator => _anim;
    public Color AimAllowed => _aimAllowed;
    public Color AimNotAllowed => _aimNotAllowed;
    public FxBank BankNormal => bankNormal;
    public FxBank BankSmall => bankSmall;
    public FxBank BankHead => bankHead;

    private void Awake()
    {
        _currentSize = PlayerSize.Normal;
        
        _thresholdPropID = Shader.PropertyToID(THRESHOLD_NAME);
        
        // Instanciamos material para poder modificarlo individualmente sin alterar el asset
        if (_bandageLine != null)
        {
            _bandageMatInst = _bandageLine.material; 
            _bandageLine.enabled = false;
        }
        
        // Inicializamos el anchor por defecto (Normal)
        _currentHandAnchor = _handAnchorNormal;
    }

    private void LateUpdate()
    {
        // Actualizamos la posición de la linea frame a frame
        // Esto debe ocurrir en LateUpdate para ir después de la animación
        if (_isBandageActive && _bandageLine != null && _currentHandAnchor != null && _bandageTarget != null)
        {
            _bandageLine.SetPosition(0, _currentHandAnchor.position);
            
            // Calculamos el punto en mundo basado en el offset local 
            // (Esto permite que la linea siga al objeto si este rota o se mueve)
            Vector3 targetWorldPos = _bandageTarget.TransformPoint(_bandageTargetLocalOffset);
            _bandageLine.SetPosition(1, targetWorldPos);
        }
    }

    // ---------------- API PÚBLICA PARA VENDAS (Swing & Attract) ----------------

    /// <summary>
    /// Inicia el visual de la venda conectando la mano actual con un punto en el mundo sobre un objeto.
    /// </summary>
    /// <param name="targetTransform">El transform del objeto (Caja, Pared, Hook)</param>
    /// <param name="worldHitPoint">El punto exacto del impacto en coordenadas de mundo</param>
    public void StartBandage(Transform targetTransform, Vector3 worldHitPoint, float duration)
    {
        if (_bandageLine == null) return;

        _bandageTarget = targetTransform;
        // Guardamos el offset local para que la linea se pegue al objeto relativo a su rotación
        _bandageTargetLocalOffset = _bandageTarget.InverseTransformPoint(worldHitPoint);
        
        _isBandageActive = true;
        _bandageLine.enabled = true;

        // Iniciar animación del shader ("Carga" visual)
        if (_drawCoroutine != null) StopCoroutine(_drawCoroutine);
        _drawCoroutine = StartCoroutine(AnimateMaterialDraw(duration));
    }

    public void StopBandage()
    {
        _isBandageActive = false;
        _bandageTarget = null;
        
        if (_bandageLine != null) 
        {
            _bandageLine.enabled = false;
            // Reseteamos el material al estado invisible/inicial
            if (_bandageMatInst) _bandageMatInst.SetFloat(_thresholdPropID, MAT_START_VAL);
        }

        if (_drawCoroutine != null) StopCoroutine(_drawCoroutine);
    }
    
    private IEnumerator AnimateMaterialDraw(float duration)
    {
        if (!_bandageMatInst) yield break;
        
        _bandageMatInst.SetFloat(_thresholdPropID, MAT_START_VAL);
        float time = 0f;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            // Lerp inverso (de 1.5 a 0) asumiendo que el shader funciona así
            float val = Mathf.Lerp(MAT_START_VAL, MAT_END_VAL, t);
            
            _bandageMatInst.SetFloat(_thresholdPropID, val);
            yield return null;
        }
        _bandageMatInst.SetFloat(_thresholdPropID, MAT_END_VAL);
    }

    // ---------------- AUDIO ----------------

    /// <summary>
    /// Reproduce un sonido del banco correspondiente al tamaño actual del jugador.
    /// </summary>
    /// <param name="key">La clave del sonido en el FxBank</param>
    /// <param name="position">Si se pasa posición, será 3D. Si es null, será 2D.</param>
    public void PlaySfx(string key, Vector3? position = null)
    {
        FxBank currentBank = GetBankForCurrentSize();

        if (currentBank == null)
        {
            Debug.LogWarning($"PlayerView: No hay un FxBank asignado para el tamaño {_currentSize}");
            return;
        }

        if (position.HasValue)
        {
            currentBank.Play3D(key, position.Value); //
        }
        else
        {
            currentBank.Play2D(key); //
        }
    }

    /// <summary>
    /// Detiene un sonido loopeable usando la clave del tamaño actual.
    /// </summary>
    public void StopSfx(string key)
    {
        GetBankForCurrentSize()?.Stop(key); // Usando el método Stop que agregamos antes
    }

    private FxBank GetBankForCurrentSize()
    {
        return _currentSize switch
        {
            PlayerSize.Normal => bankNormal,
            PlayerSize.Small  => bankSmall,
            PlayerSize.Head   => bankHead,
            _                 => null
        };
    }
    
    // ---------------- SIZE ----------------
    private void OnSizeChanged(PlayerSize newSize)
    {
        _currentSize = newSize;
        
        if (_anim == null) return;

        // 1. Guardar estados
        bool wasWalking = _anim.parameterCount > 0 && _anim.GetBool("Walk");
        bool wasIdle = _anim.parameterCount > 0 && _anim.GetBool("Idle");
        float currentSpeed = _anim.GetFloat("Speed");

        // 2. APAGAR EL ANIMATOR (Esto destruye cualquier PlayableGraph residual)
        _anim.enabled = false;

        // 3. Cambiar Controller y Avatar
        switch (newSize)
        {
            case PlayerSize.Normal:
                _anim.runtimeAnimatorController = _controllerNormal;
                _anim.avatar = _avatarNormal;
                _currentHandAnchor = _handAnchorNormal;
                break;
            case PlayerSize.Small:
                _anim.runtimeAnimatorController = _controllerSmall;
                _anim.avatar = _avatarSmall;
                _currentHandAnchor = _handAnchorSmall;
                break;
            case PlayerSize.Head:
                _anim.runtimeAnimatorController = _controllerHead;
                _anim.avatar = _avatarHead;
                _currentHandAnchor = _handAnchorHead;
                break;
        }

        // 4. ENCENDER Y REBIND
        _anim.enabled = true;
        _anim.Rebind(); 
    
        // 5. Restaurar parámetros
        if (_anim.runtimeAnimatorController != null)
        {
            _anim.SetBool("Walk", wasWalking);
            _anim.SetBool("Idle", wasIdle);
            _anim.SetFloat("Speed", currentSpeed);
        
            // 6. FORZAR EVALUACIÓN (Truco final)
            // Esto obliga al Animator a entrar al estado por defecto inmediatamente
            _anim.Play(0, -1, 0f); 
        }
    }
    public void SetMoveSpeedVisual(float normalized)
    {
        if (_anim) _anim.SetFloat("Speed", normalized);
    }

    // ---------------- HEAD UI ----------------
    public void SetHeadTimerSprite(bool isHead)
    {
        if (_headTimerFill == null) return;
        _headTimerFill.sprite = isHead ? _spriteHead : _spriteNormalOrSmall;
        _headTimerFill.fillAmount = 1f;
    }

    public void UpdateHeadTimer01(float n01)
    {
        if (_headTimerFill) _headTimerFill.fillAmount = Mathf.Clamp01(n01);
    }

    // ---------------- DROP ----------------
    private void PlayDropFx(PlayerSize playerSize)
    {
        if (_dropFX == null) return;

        var main = _dropFX.main;

        switch (playerSize)
        {
            case PlayerSize.Normal:
                main.startSize = new ParticleSystem.MinMaxCurve(4f, 5.5f);
                main.startColor = new Color(1f, 0.6f, 1f, 0.5f);
                break;

            case PlayerSize.Small:
                main.startSize = new ParticleSystem.MinMaxCurve(2f, 3.5f);
                main.startColor = new Color(0.4f, 0.6f, 1f, 0.5f);
                break;

            case PlayerSize.Head:
                main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.5f);
                main.startColor = new Color(0.6f, 1f, 1f, 0.5f);
                break;
        }

        _dropFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _dropFX.Play();
    }
    
    // ---------------- PAUSE ----------------
    public void OnPauseChanged(bool paused) => _anim.enabled = !paused;

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(OnSizeChanged);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(PlayDropFx);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(OnSizeChanged);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(PlayDropFx);
    }
}