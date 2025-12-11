using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using static PlayerEnum;

public sealed class PlayerView : MonoBehaviour, IPausable
{
    [Header("Anim & FX")]
    [SerializeField] private Animator _anim;

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

    [Header("UI (opcional)")]
    [SerializeField] private Image _headTimerFill;
    [SerializeField] private Sprite _spriteHead;
    [SerializeField] private Sprite _spriteNormalOrSmall;

    [Header("Swing Visual")]
    [SerializeField] private LineRenderer swingLine;
    [SerializeField] private Transform swingLineStart;

    private Transform _swingLineEnd;
    private bool _swingLineActive;

    [Header("Hook/Bandage Visual")]
    [Tooltip("Material que controla el reveal/dibujado de la venda/cuerda.")]

    [SerializeField] private Material _hookMaterial;
    [SerializeField] private float _bandageLaunchDelay = 0.1f;
    [SerializeField] private float _bandageDrawDuration = 0.5f;

    private const float BANDAGE_START = 1.5f;
    private const float BANDAGE_END   = 0;
    
    private Coroutine _bandageRoutine;

    private static readonly int RightThresholdId = Shader.PropertyToID("_rightThreshold");

    public GameObject Decal => _decal;
    public DecalProjector RangeIndicator => _rangeIndicator;
    public LineRenderer ArcRenderer => _arcRenderer;
    public Animator Animator => _anim;
    public Material HookMaterial => _hookMaterial;

    // ---------------- SIZE ----------------
    private void OnSizeChanged(PlayerSize newSize)
    {
        if (_anim == null) return;

        // Guardamos el estado previo para intentar mantener la fluidez
        bool wasWalking = _anim.parameterCount > 0 && _anim.GetBool("Walk");
        bool wasIdle = _anim.parameterCount > 0 && _anim.GetBool("Idle");

        switch (newSize)
        {
            case PlayerSize.Normal:
                if (_controllerNormal != null) _anim.runtimeAnimatorController = _controllerNormal;
                if (_avatarNormal != null) _anim.avatar = _avatarNormal; // <--- CAMBIO DE AVATAR
                break;

            case PlayerSize.Small:
                if (_controllerSmall != null) _anim.runtimeAnimatorController = _controllerSmall;
                if (_avatarSmall != null) _anim.avatar = _avatarSmall;   // <--- CAMBIO DE AVATAR
                break;

            case PlayerSize.Head:
                if (_controllerHead != null) _anim.runtimeAnimatorController = _controllerHead;
                if (_avatarHead != null) _anim.avatar = _avatarHead;     // <--- CAMBIO DE AVATAR
                break;
        }

        if (_anim.runtimeAnimatorController != null)
        {
            // Opcional: A veces es necesario forzar un Rebind() si los huesos se quedan "locos"
            // _anim.Rebind(); 
            
            _anim.SetBool("Walk", wasWalking);
            _anim.SetBool("Idle", wasIdle);
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

    // ---------------- SWING LINE ----------------
    public void SetSwingLineActive(bool active, Transform hookEnd = null)
    {
        _swingLineActive = active;
        _swingLineEnd = hookEnd;

        if (swingLine)
        {
            swingLine.enabled = active;
            if (active)
            {
                swingLine.positionCount = 2;
                swingLine.useWorldSpace = true;
                RefreshSwingLineNow();
            }
        }
    }

    private void RefreshSwingLineNow()
    {
        // Validación de seguridad
        if (!swingLine || !_swingLineEnd) return;
        
        swingLine.SetPosition(0, swingLineStart.position);
        swingLine.SetPosition(1, _swingLineEnd.position);
    }

    private void FixedUpdate()
    {
        if (_swingLineActive) RefreshSwingLineNow();
    }

    // ---------------- BANDAGE DRAW (VISUAL ONLY) ----------------
    public void PlayBandageDraw(Action onAttachMoment = null, float? launchDelayOverride = null, float? drawDurationOverride = null)
    {
        if (_bandageRoutine != null)
            StopCoroutine(_bandageRoutine);

        float delay = launchDelayOverride ?? _bandageLaunchDelay;
        float dur   = drawDurationOverride ?? _bandageDrawDuration;

        _bandageRoutine = StartCoroutine(BandageRoutine(delay, dur, onAttachMoment));
    }

    public void CancelBandageDraw()
    {
        if (_bandageRoutine != null)
        {
            StopCoroutine(_bandageRoutine);
            _bandageRoutine = null;
        }
    }

    private IEnumerator BandageRoutine(float delay, float drawDuration, Action onAttachMoment)
    {
        if (_hookMaterial == null)
        {
            onAttachMoment?.Invoke();
            _bandageRoutine = null;
            yield break;
        }

        _hookMaterial.SetFloat(RightThresholdId, BANDAGE_START);

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float time = 0f;
        bool fired = false;

        while (time < drawDuration)
        {
            time += Time.deltaTime;

            float newValue = Mathf.Lerp(BANDAGE_START, BANDAGE_END, time / drawDuration);

            _hookMaterial.SetFloat(RightThresholdId, newValue);

            if (!fired && newValue <= 0f)
            {
                fired = true;
                onAttachMoment?.Invoke();
            }

            yield return null;
        }


        _hookMaterial.SetFloat(RightThresholdId, BANDAGE_END);
        _bandageRoutine = null;
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

        CancelBandageDraw();
    }
}
