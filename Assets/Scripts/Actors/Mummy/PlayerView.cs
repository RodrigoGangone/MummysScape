using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static PlayerEnum;

/// <summary> 
/// Gestor Audiovisual: Coordina el cambio de animadores, avatares y bancos de sonido según el tamaño, 
/// además de gestionar visuales complejos como el dibujo de las vendas y efectos de partículas. 
/// </summary>
public sealed class PlayerView : MonoBehaviour, IPausable
{
    [Header("Anim & FX")] [SerializeField] private Animator _anim;

    [SerializeField] private FxBank bankNormal;
    [SerializeField] private FxBank bankSmall;
    [SerializeField] private FxBank bankHead;

    [SerializeField] private RuntimeAnimatorController _controllerNormal;
    [SerializeField] private RuntimeAnimatorController _controllerSmall;
    [SerializeField] private RuntimeAnimatorController _controllerHead;

    [SerializeField] private Avatar _avatarNormal;
    [SerializeField] private Avatar _avatarSmall;
    [SerializeField] private Avatar _avatarHead;

    [SerializeField] public ParticleSystem _smashFX;
    [SerializeField] public ParticleSystem _dropFX;
    [SerializeField] public ParticleSystem _koFX;
    [SerializeField] public ParticleSystem fallingDust;

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

    [Header("Feedback System")] [SerializeField]
    private PlayerFeedbackLibrary _feedbackLibrary;

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

    public GameObject Decal => _decal;
    public DecalProjector Shadow => shadow;
    public DecalProjector RangeIndicator => _rangeIndicator;
    public LineRenderer ArcRenderer => _arcRenderer;
    public Animator Animator => _anim;
    public Color AimAllowed => _aimAllowed;
    public Color AimNotAllowed => _aimNotAllowed;
    public Transform handAnchor => _handAnchorNormal;

    private void Awake()
    {
        _currentBank = bankNormal;

        _thresholdPropID = Shader.PropertyToID(THRESHOLD_NAME);

        if (_bandageLine != null)
        {
            _bandageMatInst = _bandageLine.material;
            _bandageLine.enabled = false;
        }

        _currentHandAnchor = _handAnchorNormal;
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
            _lastCutMidPoint = (startPos + targetWorldPos) * 0.5f;        }
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
            _ => null
        };
    }

    private void OnSizeChanged(PlayerSize newSize)
    {
        if (_anim == null) return;

        bool wasWalking = _anim.parameterCount > 0 && _anim.GetBool("Walk");
        bool wasIdle = _anim.parameterCount > 0 && _anim.GetBool("Idle");
        //float currentSpeed = _anim.GetFloat("Speed");

        _anim.enabled = false;

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

        _anim.enabled = true;
        _anim.Rebind();
        _currentSize = newSize;
        _currentBank = GetBankForCurrentSize(newSize);

        if (_anim.runtimeAnimatorController != null)
        {
            _anim.SetBool("Walk", wasWalking);
            _anim.SetBool("Idle", wasIdle);
            //_anim.SetFloat("Speed", currentSpeed);

            _anim.Play(0, -1, 0f);
        }
    }

    public void SetMoveSpeedVisual(float normalized)
    {
        //if (_anim) _anim.SetFloat("Speed", normalized);
    }

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

    public void HandleFailedTransition(PlayerStateId state, PlayerSize size, PlayerContext ctx)
    {
        if (_feedbackLibrary != null)
            _feedbackLibrary.Execute(state, size, ctx);
    }

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

    public void CutBandage()
    {
        if (_cutFx == null) return;

        _cutFx.transform.position = _lastCutMidPoint;
        _cutFx.Play();
    }
}