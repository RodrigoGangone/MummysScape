using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using static PlayerEnum;

/// <summary> 
/// Gestor Audiovisual: Coordina el cambio de animadores, avatares y bancos de sonido según el tamaño, 
/// además de gestionar visuales complejos como el dibujo de las vendas, efectos de partículas y colores de materiales.
/// </summary>
public sealed class PlayerView : MonoBehaviour, IPausable
{
    [Header("Animators (Asignar cada malla individual)")] [SerializeField]
    private Animator _animNormal;

    [SerializeField] private Animator _animSmall;
    [SerializeField] private Animator _animHead;
    [SerializeField] private Animator _animEmpowered;

    [Header("FX Banks")] [SerializeField] private FxBank bankNormal;
    [SerializeField] private FxBank bankSmall;
    [SerializeField] private FxBank bankHead;
    [SerializeField] private FxBank bankEmpowered;

    [Header("Particles")] [SerializeField] public ParticleSystem _smashFX;
    [SerializeField] public ParticleSystem _dropFX;
    [SerializeField] public ParticleSystem _koFX;
    [SerializeField] public ParticleSystem fallingDust;
    [SerializeField] public ParticleSystem angerFx;
    [SerializeField] private DecalProjector shadow;

    [Header("Shoot Visual")] [SerializeField]
    private GameObject _decal;

    [SerializeField] private DecalProjector _rangeIndicator;
    [SerializeField] private LineRenderer _arcRenderer;

    [ColorUsage(true, true), SerializeField]
    private Color _aimAllowed;

    [ColorUsage(true, true), SerializeField]
    private Color _aimNotAllowed;

    [Header("Bandage Visuals System")] [SerializeField]
    private LineRenderer _bandageLine;

    [SerializeField] private ParticleSystem _cutFx;

    [Header("Hand Anchors (Asignar transforms de las manos)")] [SerializeField]
    private Transform _handAnchorNormal;

    [SerializeField] private Transform _handAnchorSmall;
    [SerializeField] private Transform _handAnchorHead;
    [SerializeField] private Transform _handAnchorEmpowered;

    [Header("Feedback System")] [SerializeField]
    private PlayerFeedbackLibrary _feedbackLibrary;

    [Header("Color Settings (Scriptable Objects)")] [SerializeField]
    private MummyColorSetSO _colorSetNormal;

    [SerializeField] private MummyColorSetSO _colorSetSmall;
    [SerializeField] private MummyColorSetSO _colorSetHead;
    [SerializeField] private MummyColorSetSO _colorSetEmpowered;

    [Header("Direct Material Assets")] [FormerlySerializedAs("_fire1Renderer")] [SerializeField]
    private Material _fire1Mat;

    [FormerlySerializedAs("_fire2Renderer")] [SerializeField]
    private Material _fire2Mat;

    [SerializeField] private Material _mainMaterial1;
    [SerializeField] private Material _mainMaterial2;

    [SerializeField] private Animator _currentAnim; // Referencia interna al animator activo
    private Transform _currentHandAnchor;
    private Transform _bandageTarget;
    private Vector3 _bandageTargetLocalOffset;
    private bool _isBandageActive;

    private Coroutine _drawCoroutine;
    private Material _bandageMatInst;
    private int _thresholdPropID;
    private const string THRESHOLD_NAME = "_rightThreshold";
    private const float MAT_START_VAL = 1.5f;
    private const float MAT_END_VAL = 0f;

    private PlayerSize _currentSize = PlayerSize.Normal;
    private FxBank _currentBank;
    private Vector3 _lastCutMidPoint;

    // IDs de propiedades de Shaders cacheados
    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int BottomColorProp = Shader.PropertyToID("_BottomColor");
    private static readonly int MidColorProp = Shader.PropertyToID("_MidColor");
    private static readonly int TopColorProp = Shader.PropertyToID("_TopColor");
    private static readonly int GlowColorProp = Shader.PropertyToID("_GlowColor");
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    public GameObject Decal => _decal;
    public DecalProjector Shadow => shadow;
    public DecalProjector RangeIndicator => _rangeIndicator;
    public LineRenderer ArcRenderer => _arcRenderer;

    // Tus PlayerStates accederán siempre al Animator activo a través de esta propiedad
    public Animator Animator => _currentAnim;

    public Color AimAllowed => _aimAllowed;
    public Color AimNotAllowed => _aimNotAllowed;
    public Transform handAnchor => _currentHandAnchor;

    private void Awake()
    {
        //_currentBank = bankNormal;
        //_currentAnim = _animNormal; // Default
        //_currentHandAnchor = _handAnchorNormal; // Default

        _thresholdPropID = Shader.PropertyToID(THRESHOLD_NAME);

        if (_bandageLine != null)
        {
            _bandageMatInst = _bandageLine.material;
            _bandageLine.enabled = false;
        }

        ApplyColorSet(_colorSetNormal);
    }

    private void LateUpdate()
    {
        if (_isBandageActive && _bandageLine != null && _currentHandAnchor != null && _bandageTarget != null)
        {
            // 1. Obtener la posición inicial (Mano)
            Vector3 startPos = _currentHandAnchor.position;

            // 2. Obtener la posición final (Objetivo)
            Vector3 targetWorldPos = _bandageTarget.TransformPoint(_bandageTargetLocalOffset);

            // 3. Actualizar el LineRenderer
            _bandageLine.SetPosition(0, startPos);
            _bandageLine.SetPosition(1, targetWorldPos);

            // 4. Calcular el punto medio para el efecto de corte
            _lastCutMidPoint = (startPos + targetWorldPos) * 0.5f;
        }
    }

    public void StartBandage(Transform targetTransform, Vector3 worldHitPoint, float duration)
    {
        if (_bandageLine == null) return;

        _bandageTarget = targetTransform;
        _bandageTargetLocalOffset = _bandageTarget.InverseTransformPoint(worldHitPoint);

        _isBandageActive = true;
        _bandageLine.enabled = true;

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
            float val = Mathf.Lerp(MAT_START_VAL, MAT_END_VAL, t);

            _bandageMatInst.SetFloat(_thresholdPropID, val);
            yield return null;
        }

        _bandageMatInst.SetFloat(_thresholdPropID, MAT_END_VAL);
    }

    public void PlaySfx(string key, Vector3? position = null)
    {
        if (_currentBank == null) _currentBank = GetBankForCurrentSize();

        if (position.HasValue)
            _currentBank.Play3D(key, position.Value);
        else
            _currentBank.Play2D(key);
    }

    public void StopSfx(string key)
    {
        if (_currentBank == null) _currentBank = GetBankForCurrentSize();

        _currentBank?.Stop(key);
    }

    private FxBank GetBankForCurrentSize(PlayerSize? size = null)
    {
        PlayerSize targetSize = size ?? _currentSize;

        return targetSize switch
        {
            PlayerSize.Normal => bankNormal,
            PlayerSize.Small => bankSmall,
            PlayerSize.Head => bankHead,
            PlayerSize.Empowered => bankEmpowered,
            _ => null
        };
    }

    private void ApplyColorSet(MummyColorSetSO colorSet)
    {
        if (colorSet == null) return;

        if (_fire1Mat != null)
        {
            _fire1Mat.SetColor(BottomColorProp, colorSet.fire1Bottom);
            _fire1Mat.SetColor(MidColorProp, colorSet.fire1Mid);
            _fire1Mat.SetColor(TopColorProp, colorSet.fire1Top);
        }

        if (_fire2Mat != null)
        {
            _fire2Mat.SetColor(BottomColorProp, colorSet.fire2Bottom);
            _fire2Mat.SetColor(MidColorProp, colorSet.fire2Mid);
            _fire2Mat.SetColor(TopColorProp, colorSet.fire2Top);
            _fire2Mat.SetColor(GlowColorProp, colorSet.fire2Glow);
        }

        if (_mainMaterial1 != null)
            _mainMaterial1.SetColor(EmissionColorProp, colorSet.skull);

        if (_mainMaterial2 != null)
            _mainMaterial2.SetColor(EmissionColorProp, colorSet.skull);
    }

    private void OnSizeChanged(PlayerSize newSize)
    {
        // Rescatamos el estado del Animator anterior antes de cambiarlo
        bool wasWalking = _currentAnim != null && _currentAnim.parameterCount > 0 && _currentAnim.GetBool("Walk");
        bool wasIdle = _currentAnim != null && _currentAnim.parameterCount > 0 && _currentAnim.GetBool("Idle");

        switch (newSize)
        {
            case PlayerSize.Normal:
                _currentAnim = _animNormal;
                _currentHandAnchor = _handAnchorNormal;
                ApplyColorSet(_colorSetNormal);
                break;
            case PlayerSize.Small:
                _currentAnim = _animSmall;
                _currentHandAnchor = _handAnchorSmall;
                ApplyColorSet(_colorSetSmall);
                break;
            case PlayerSize.Head:
                _currentAnim = _animHead;
                _currentHandAnchor = _handAnchorHead;
                ApplyColorSet(_colorSetHead);
                break;
            case PlayerSize.Empowered:
                _currentAnim = _animEmpowered;
                _currentHandAnchor = _handAnchorEmpowered;
                ApplyColorSet(_colorSetEmpowered);
                break;
        }

        _currentSize = newSize;
        _currentBank = GetBankForCurrentSize(newSize);

        // Si existe el nuevo Animator, le pasamos los estados que tenía el anterior
        if (_currentAnim != null)
        {
            _currentAnim.SetBool("Walk", wasWalking);
            _currentAnim.SetBool("Idle", wasIdle);
        }
    }

    public void SetMoveSpeedVisual(float normalized)
    {
        //if (_currentAnim) _currentAnim.SetFloat("Speed", normalized);
    }

    private void PlayDropFx(PlayerSize playerSize)
    {
        if (_dropFX == null) return;

        if (!_dropFX.gameObject.activeSelf)
        {
            _dropFX.gameObject.SetActive(true);
            return;
        }

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
            case PlayerSize.Empowered:
                main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.5f);
                main.startColor = new Color(1f, 1f, 0f, 0.5f);
                break;
        }

        _dropFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _dropFX.Play();
    }

    public void HandleFailedTransition(PlayerStateId state, PlayerSize size, PlayerContext ctx)
    {
        if (_feedbackLibrary != null)
            _feedbackLibrary.Execute(state, size, ctx);
    }

    public void OnPauseChanged(bool paused)
    {
        if (_currentAnim != null)
            _currentAnim.enabled = !paused;
    }

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

    public void CutBandage()
    {
        if (_cutFx == null) return;

        _cutFx.transform.position = _lastCutMidPoint;
        _cutFx.Play();
    }
}