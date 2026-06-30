using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Video;
using static Tags;
using static PauseUtils;
using static SfxIDs;
using DragonBones;

/// <summary> 
/// Controlador de Tutorial: Gestiona la activación de tutoriales en escena, coordinando efectos visuales, 
/// videos y la validación de persistencia para evitar la repetición de guías ya completadas. 
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TutorialTrigger : MonoBehaviour, IPausable
{
    [Header("Referencias")] [SerializeField]
    private TutorialFocusPoint focusPoint;

    [SerializeField] private ParticleSystem[] braziers;
    [SerializeField] private string _animationDBName;

    [Header("Configuración de Áreas")] [SerializeField]
    private Vector3 sizeA = Vector3.one;

    [SerializeField] private Vector3 sizeB = Vector3.one * 2f;
    [SerializeField] private Vector3 centerOffsetA = Vector3.zero;
    [SerializeField] private Vector3 centerOffsetB = Vector3.zero;

    [Header("Audio")] [SerializeField] private FxBank _bank;

    private BoxCollider _boxCollider;
    [SerializeField] private UnityArmatureComponent _armatureComponent;
    private Coroutine _effectRoutine;
    private bool _isPromptActive;
    private bool _paused;
    private bool _isPlaying;
    private bool _canPlayTutorial;

    private bool IsTutorialAlreadySeen => focusPoint != null && Save.IsTutorialSeen(focusPoint.Tutorial);

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();

        if (focusPoint != null)
            SetColliderShape(!IsTutorialAlreadySeen);
    }

    private void Start()
    {
        _armatureComponent.animation.Stop(_animationDBName);
        ToggleEffects(false);
    }

    private void Update()
    {
        if (_paused || FocusManager.Instance == null)
            return;

        if (_canPlayTutorial && !_isPlaying && Input.GetButtonDown(FocusManager.Instance.TutorialKey))
        {
            StartCoroutine(StartReplayWithDelay());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG) || focusPoint == null) return;

        // NUEVO: Bloqueamos la re-evaluación provocada por el SetColliderShape
        // Si el tutorial ya arrancó, ignoramos este falso Enter.
        if (_isPlaying) return;

        // SOLUCIÓN 1: Siempre habilitamos que pueda jugar el tutorial al entrar.
        _canPlayTutorial = true;

        if (!IsTutorialAlreadySeen)
        {
            ExecuteTutorialSequence(isReplay: false);
            SetColliderShape(false);
            return;
        }

        if (!_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                ContextUIFactory.Prompt(ContextMessageType.ReplayTutorial, ButtonType.Y)
            );
            _isPromptActive = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;

        // NUEVO: Evitamos que el resize apague las variables mientras 
        // la cinemática se está reproduciendo y el jugador está quieto.
        if (_isPlaying) return;

        _canPlayTutorial = false;
        _isPromptActive = false;

        GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
            ContextUIFactory.Hidden()
        );
    }
    // SOLUCIÓN 3: Esperamos al final del frame para que el botón "Y" se suelte,
    // evitando que el FocusManager cancele el tutorial instantáneamente.
    private IEnumerator StartReplayWithDelay()
    {
        _isPlaying = true; // Bloqueamos el input inmediatamente
        yield return new WaitForEndOfFrame();
        ExecuteTutorialSequence(isReplay: true);
    }

    private void ExecuteTutorialSequence(bool isReplay)
    {
        if (focusPoint == null || FocusManager.Instance == null) return;

        _bank?.Play2D(Tutorial.See);
        _isPlaying = true;

        if (_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                ContextUIFactory.Hidden()
            );
            _isPromptActive = false;
        }

        FocusManager.Instance.RequestTutorial(
            focusPoint,
            isReplay ? OnTutorialReplayCancelled : null
        );

        if (_effectRoutine != null)
            StopCoroutine(_effectRoutine);

        float totalDuration = focusPoint.Time;

        if (!isReplay && !string.IsNullOrEmpty(focusPoint.Message))
            totalDuration += focusPoint.MessageDuration;

        // Fallback de seguridad: Si el replay dura 0 segundos, le damos un tiempo base.
        if (totalDuration <= 0.1f)
            totalDuration = 3f;

        _effectRoutine = StartCoroutine(TutorialDurationRoutine(totalDuration));
    }

    private IEnumerator TutorialDurationRoutine(float duration)
    {
        ToggleEffects(true);

        yield return WaitForSecondsPausable(duration, () => _paused);

        ToggleEffects(false);

        _effectRoutine = null;
        _isPlaying = false;

        if (_canPlayTutorial && IsTutorialAlreadySeen && !_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                ContextUIFactory.Prompt(ContextMessageType.ReplayTutorial, ButtonType.Y)
            );
            _isPromptActive = true;
        }
    }

    private void OnTutorialReplayCancelled()
    {
        if (_effectRoutine != null)
        {
            StopCoroutine(_effectRoutine);
            _effectRoutine = null;
        }

        ToggleEffects(false);
        _isPlaying = false;

        if (_canPlayTutorial && !_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                ContextUIFactory.Prompt(ContextMessageType.ReplayTutorial, ButtonType.Y)
            );
            _isPromptActive = true;
        }
    }

    private void ToggleEffects(bool active)
    {
        foreach (var brazier in braziers)
        {
            if (brazier == null) continue;

            if (active) brazier.Play();
            else brazier.Stop();
        }

        if (_armatureComponent != null)
        {
            if (active)
            {
                _armatureComponent.animation.Play(_animationDBName, 1);
            }
            else
            {
                _armatureComponent.animation.Reset();
                _armatureComponent.animation.Stop();
            }
        }
    }

    private void SetColliderShape(bool useSizeA)
    {
        if (_boxCollider == null) return;

        _boxCollider.size = useSizeA ? sizeA : sizeB;
        _boxCollider.center = useSizeA ? centerOffsetA : centerOffsetB;
    }

    public void OnPauseChanged(bool paused) => _paused = paused;

    private void OnEnable() =>
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);

    private void OnDisable() =>
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (focusPoint == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = !IsTutorialAlreadySeen ? Color.green : new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireCube(centerOffsetA, sizeA);

        Gizmos.color = IsTutorialAlreadySeen ? Color.yellow : new Color(1, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireCube(centerOffsetB, sizeB);
    }

    #endregion
}