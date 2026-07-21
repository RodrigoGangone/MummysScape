using System.Collections;
using UnityEngine;
using static Tags;
using static PauseUtils;
using static SfxIDs;
using DragonBones;

/// <summary> 
/// Controlador de Tutorial: Simula triggers mediante OverlapBox para evitar fallos de físicas.
/// Versión optimizada y centralizada.
/// </summary>
public class TutorialTrigger : MonoBehaviour, IPausable
{
    [Header("Referencias")] [SerializeField]
    private TutorialFocusPoint focusPoint;

    [SerializeField] private ParticleSystem[] braziers;
    [SerializeField] private string _animationDBName;
    [SerializeField] private UnityArmatureComponent _armatureComponent;

    [Header("Configuración de Áreas (Simuladas)")] [SerializeField]
    private Vector3 sizeA = Vector3.one;

    [SerializeField] private Vector3 sizeB = Vector3.one * 2f;
    [SerializeField] private Vector3 centerOffsetA = Vector3.zero;
    [SerializeField] private Vector3 centerOffsetB = Vector3.zero;
    [SerializeField] private LayerMask playerLayerMask = ~0;

    [Header("Audio")] [SerializeField] private FxBank _bank;

    private Coroutine _effectRoutine;
    private bool _isPromptActive;
    private bool _paused;
    private bool _isPlaying;
    private bool _firstView;
    private bool _wasPlayerInside;

    private readonly Collider[] _overlapResults = new Collider[2];

    private bool IsTutorialAlreadySeen => focusPoint != null && Save.IsTutorialSeen(focusPoint.Tutorial);

    private void Awake() => _firstView = !IsTutorialAlreadySeen;

    private void Start()
    {
        _armatureComponent.animation.GotoAndStopByFrame(_animationDBName, 0);

        ToggleEffects(false);
    }

    private void FixedUpdate()
    {
        if (_paused || FocusManager.Instance == null)
            return;

        EvaluatePlayerInArea();
    }

    private void Update()
    {
        if (_paused || FocusManager.Instance == null)
            return;

        if (_wasPlayerInside && !_isPlaying && Input.GetButtonDown(FocusManager.Instance.TutorialKey))
            StartCoroutine(StartReplayWithDelay());
    }

    private void EvaluatePlayerInArea()
    {
        Vector3 size = _firstView ? sizeA : sizeB;
        Vector3 worldCenter = transform.TransformPoint(_firstView ? centerOffsetA : centerOffsetB);
        Vector3 halfExtents = Vector3.Scale(size, transform.lossyScale) * 0.5f;

        int hitCount = Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, _overlapResults, transform.rotation,
            playerLayerMask);

        bool isInside = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (_overlapResults[i].CompareTag(PLAYER_TAG))
            {
                isInside = true;
                break;
            }
        }

        if (isInside && !_wasPlayerInside) OnPlayerEnter();
        else if (!isInside && _wasPlayerInside) OnPlayerExit();

        _wasPlayerInside = isInside;
    }

    private void OnPlayerEnter()
    {
        if (focusPoint == null || _isPlaying)
            return;

        if (!IsTutorialAlreadySeen)
        {
            ExecuteTutorialSequence(isReplay: false);
            _firstView = false;
        }
        else
            TogglePrompt(true);
    }

    private void OnPlayerExit()
    {
        if (_isPlaying)
            return;

        TogglePrompt(false);
    }

    private IEnumerator StartReplayWithDelay()
    {
        _isPlaying = true;
        yield return new WaitForEndOfFrame();
        ExecuteTutorialSequence(isReplay: true);
    }

    private void ExecuteTutorialSequence(bool isReplay)
    {
        if (focusPoint == null || FocusManager.Instance == null)
            return;

        _bank?.Play2D(Tutorial.See);
        _isPlaying = true;
        TogglePrompt(false);

        FocusManager.Instance.RequestTutorial(focusPoint, EndTutorial);

        if (_effectRoutine != null) StopCoroutine(_effectRoutine);

        float duration = focusPoint.Time +
                         (!isReplay && !string.IsNullOrEmpty(focusPoint.Message) ? focusPoint.MessageDuration : 0);
        _effectRoutine = StartCoroutine(TutorialDurationRoutine(duration <= 0.1f ? 3f : duration));
    }

    private IEnumerator TutorialDurationRoutine(float duration)
    {
        ToggleEffects(true);
        yield return WaitForSecondsPausable(duration, () => _paused);
        EndTutorial();
    }

    private void EndTutorial()
    {
        if (_effectRoutine != null)
            StopCoroutine(_effectRoutine);

        _effectRoutine = null;
        _isPlaying = false;
        ToggleEffects(false);

        if (_wasPlayerInside)
            TogglePrompt(true);
    }

    private void TogglePrompt(bool show)
    {
        if (_isPromptActive == show)
            return;

        _isPromptActive = show;
        var message = show
            ? ContextUIFactory.Prompt(ContextMessageType.ReplayTutorial, ButtonType.Y)
            : ContextUIFactory.Hidden();
        GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(message);
    }

    private void ToggleEffects(bool active)
    {
        if (braziers != null)
        {
            foreach (var b in braziers)
            {
                // Safety check: Skip this iteration if the array element is empty
                if (b == null) 
                    continue;

                if (active) b.Play();
                else b.Stop();
            }
        }

        if (active)
            _armatureComponent.animation.Play(_animationDBName, 1);
        else
            _armatureComponent.animation.GotoAndStopByFrame(_animationDBName, 0);
    }

    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (focusPoint == null) return;
        Gizmos.matrix = transform.localToWorldMatrix;

        bool isA_Active = Application.isPlaying ? _firstView : !IsTutorialAlreadySeen;

        Gizmos.color = isA_Active ? Color.green : new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireCube(centerOffsetA, sizeA);

        Gizmos.color = !isA_Active ? Color.yellow : new Color(1, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireCube(centerOffsetB, sizeB);
    }

    #endregion
}