using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Video;
using static PauseUtils;

[RequireComponent(typeof(BoxCollider))]
public class TutorialTrigger : MonoBehaviour, IPausable
{
    [Header("Referencias")] [SerializeField]
    private TutorialFocusPoint focusPoint;

    [SerializeField] private ParticleSystem[] braziers;
    private VideoPlayer _tutorialVideo;

    [Header("Configuración de Áreas")] [SerializeField]
    private Vector3 sizeA = Vector3.one;

    [SerializeField] private Vector3 sizeB = Vector3.one * 2f;
    [SerializeField] private Vector3 centerOffsetA = Vector3.zero;
    [SerializeField] private Vector3 centerOffsetB = Vector3.zero;

    private BoxCollider _boxCollider;
    private bool _isSizeA = true;
    private bool _isPromptActive;
    private Coroutine _effectRoutine;
    private bool _paused;
    private bool _isPlaying;
    
    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _tutorialVideo = GetComponentInChildren<VideoPlayer>();

        SetColliderShape(true);
        ToggleEffects(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather") || !_isSizeA) return;

        if (!Save.IsTutorialSeen(focusPoint.Id))
            ExecuteTutorialSequence(focusPoint.Time);

        SetColliderShape(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("PlayerFather") || _isSizeA) return;

        if (!_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.Y);
            _isPromptActive = true;
        }

        if (!_paused && !_isPlaying &&Input.GetButtonDown(FocusManager.Instance.TutorialKey))
        {
            ExecuteTutorialSequence(focusPoint.Time);
            _isPlaying = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.Y);
        _isPromptActive = false;
    }

    private void ExecuteTutorialSequence(float tutorialTime)
    {
        Debug.Log("TutorialTrigger");
        
        FocusManager.Instance.RequestTutorial(focusPoint);

        if (_effectRoutine != null) StopCoroutine(_effectRoutine);

        _effectRoutine = StartCoroutine(TutorialDurationRoutine(tutorialTime));
    }

    private IEnumerator TutorialDurationRoutine(float time)
    {
        ToggleEffects(true);

        yield return WaitForSecondsPausable(time, () => _paused);

        ToggleEffects(false);

        _effectRoutine = null;
        _isPlaying = false;
    }

    private void ToggleEffects(bool active)
    {
        foreach (var brazier in braziers)
        {
            if (brazier == null) continue;

            if (active) brazier.Play();
            else brazier.Stop();
        }

        if (_tutorialVideo != null)
        {
            if (active) _tutorialVideo.Play();
            else _tutorialVideo.Stop();
        }
    }

    private void SetColliderShape(bool useSizeA)
    {
        _isSizeA = useSizeA;
        _boxCollider.size = useSizeA ? sizeA : sizeB;
        _boxCollider.center = useSizeA ? centerOffsetA : centerOffsetB;
    }

    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);

    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

    #region Gizmo

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = _isSizeA ? Color.green : new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireCube(centerOffsetA, sizeA);
        Gizmos.color = !_isSizeA ? Color.yellow : new Color(1, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireCube(centerOffsetB, sizeB);
    }

    #endregion
}