using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using static PlayerEnum; // <-- IMPORTANTE

/// <summary>
/// PlayerView
/// Solo visual: Animator/FX/UI. Sin reglas de juego.
/// </summary>
public sealed class PlayerView : MonoBehaviour, IPausable
{
    [Header("Anim & FX")]
    [SerializeField] private Animator _anim;
    
    [SerializeField] private RuntimeAnimatorController _controllerNormal;
    [SerializeField] private RuntimeAnimatorController _controllerSmall;
    [SerializeField] private RuntimeAnimatorController _controllerHead;
    
    [SerializeField] public ParticleSystem _shootFX;
    [SerializeField] public ParticleSystem _smashFX;
    
    [Header("Shoot Visual")]
    [SerializeField] private GameObject _decal;
    [SerializeField] private DecalProjector _rangeIndicator;
    [SerializeField] private LineRenderer _arcRenderer;
    
    [Header("UI (opcional)")]
    [SerializeField] private Image _headTimerFill; 
    [SerializeField] private Sprite _spriteHead;
    [SerializeField] private Sprite _spriteNormalOrSmall;
    
    [Header("Swing Visual")]
    [Tooltip("LineRenderer usado para dibujar la cuerda del hook.")]
    [SerializeField] private LineRenderer _swingLine;
    [Tooltip("Punto de salida visual de la cuerda. Si es null se usa el transform del player.")]
    [SerializeField] private Transform _swingLineStart;

    private Transform _swingLineEnd;   // hook (se asigna en SwingState)
    private bool _swingLineActive;
    
    public GameObject Decal => _decal;
    public DecalProjector RangeIndicator => _rangeIndicator;
    public LineRenderer ArcRenderer => _arcRenderer;
    public Animator Animator => _anim;

    /// <summary>
    /// Método que se suscribe al evento OnSizeChanged.
    /// Cambia el asset del Animator en tiempo de ejecución.
    /// </summary>
    public void OnSizeChanged(PlayerSize newSize)
    {
        if (_anim == null) return;

        // 1. GUARDAR ESTADO ACTUAL
        // Es importante verificar si el animator está inicializado para evitar errores
        bool wasWalking = _anim.parameterCount > 0 && _anim.GetBool("Walk");
        bool wasIdle = _anim.parameterCount > 0 && _anim.GetBool("Idle");

        // 2. CAMBIAR CONTROLLER
        switch (newSize)
        {
            case PlayerSize.Normal:
                if (_controllerNormal != null) _anim.runtimeAnimatorController = _controllerNormal;
                break;
            case PlayerSize.Small:
                if (_controllerSmall != null) _anim.runtimeAnimatorController = _controllerSmall;
                break;
            case PlayerSize.Head:
                if (_controllerHead != null) _anim.runtimeAnimatorController = _controllerHead;
                break;
        }

        // 3. RESTAURAR ESTADO
        // Unity a veces tarda un frame en reinicializar los parámetros tras el cambio,
        // pero usualmente reasignarlos inmediatamente funciona si los nombres coinciden.
        if (_anim.runtimeAnimatorController != null)
        {
            _anim.SetBool("Walk", wasWalking);
            _anim.SetBool("Idle", wasIdle);
        }
    }
    
    public void SetMoveSpeedVisual(float normalized)
    {
        if (_anim) _anim.SetFloat("Speed", normalized);
    }
    
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
    
    
    /// <summary>
    /// Activa/Desactiva la cuerda del swing. Al activar, se pasa el transform del hook.
    /// </summary>
    public void SetSwingLineActive(bool active, Transform hookEnd = null)
    {
        _swingLineActive = active;
        _swingLineEnd = hookEnd;

        if (_swingLine)
        {
            _swingLine.enabled = active;
            if (active)
            {
                _swingLine.positionCount = 2;
                _swingLine.useWorldSpace = true;
                RefreshSwingLineNow();
            }
        }
    }

    /// <summary>
    /// Refresca la posición de la cuerda (player -> hook). Llamado en LateUpdate para quedar al final del frame.
    /// </summary>
    private void RefreshSwingLineNow()
    {
        if (!_swingLineActive || !_swingLine || !_swingLineEnd) return;

        var start = _swingLineStart ? _swingLineStart.position : transform.position;
        var end   = _swingLineEnd.position;

        _swingLine.SetPosition(0, start);
        _swingLine.SetPosition(1, end);
    }

    private void LateUpdate()
    {
        if (_swingLineActive) RefreshSwingLineNow();
    }

    public void OnPauseChanged(bool paused) => _anim.enabled = !paused;
    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(OnSizeChanged);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(OnSizeChanged);
    }
}