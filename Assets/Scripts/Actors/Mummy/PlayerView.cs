using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal; // <-- IMPORTANTE

/// <summary>
/// PlayerView
/// Solo visual: Animator/FX/UI. Sin reglas de juego.
/// </summary>
public sealed class PlayerView : MonoBehaviour, IPausable
{
    [Header("Anim & FX")]
    [SerializeField] private Animator _anim;
    [SerializeField] private ParticleSystem _shootFX;
    [SerializeField] private ParticleSystem _smashFX;
    
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

    public void SetMoveSpeedVisual(float normalized)
    {
        if (_anim) _anim.SetFloat("Speed", normalized);
    }

    public void PlayShoot()
    {
        _anim?.SetTrigger("Shoot");
        if (_shootFX && !_shootFX.isPlaying) _shootFX.Play();
    }

    public void PlaySmash()
    {
        _anim?.SetTrigger("Smash");
        if (_smashFX && !_smashFX.isPlaying) _smashFX.Play();
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
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}