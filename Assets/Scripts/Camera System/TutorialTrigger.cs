using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Referencias")] [SerializeField]
    private TutorialFocusPoint focusPoint;

    [SerializeField] private FirePillar[] pillars;
    [SerializeField] private Material tutorialMaterial;

    [Header("Configuración de Áreas")] [SerializeField]
    private Vector3 sizeA = Vector3.one;

    [SerializeField] private Vector3 sizeB = Vector3.one * 2f;
    [SerializeField] private Vector3 centerOffsetA = Vector3.zero;
    [SerializeField] private Vector3 centerOffsetB = Vector3.zero;

    private BoxCollider _boxCollider;
    private bool _isSizeA = true;
    private bool _isPromptActive;
    private Coroutine _effectRoutine;

    private static readonly int PlayShaderProp = Shader.PropertyToID("_play");

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        SetColliderShape(true);
        ToggleEffects(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        if (!_isSizeA || FocusManager.Instance == null) return;
        
        if (!Save.IsTutorialSeen(focusPoint.Id))
            ExecuteTutorialSequence();

        SetColliderShape(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        if (_isSizeA) return;

        if (!_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.Y);
            _isPromptActive = true;
        }

        if (FocusManager.Instance != null && Input.GetButtonDown(FocusManager.Instance.TutorialKey))
            ExecuteTutorialSequence();
    }

    private void ExecuteTutorialSequence()
    {
        if (FocusManager.Instance == null) return;

        FocusManager.Instance.RequestTutorial(focusPoint);

        if (_effectRoutine != null) StopCoroutine(_effectRoutine);

        _effectRoutine = StartCoroutine(TutorialDurationRoutine());
    }

    private IEnumerator TutorialDurationRoutine()
    {
        ToggleEffects(true);

        yield return new WaitForSeconds(focusPoint.MandatoryTime);

        ToggleEffects(false);

        _effectRoutine = null;
    }

    private void ToggleEffects(bool active)
    {
        if (pillars != null)
            foreach (var pillar in pillars)
                if (pillar != null)
                    pillar.SetState(active);

        if (tutorialMaterial != null)
            tutorialMaterial.SetFloat(PlayShaderProp, active ? 1.0f : 0.0f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        ForceClosePrompt();
    }

    private void SetColliderShape(bool useSizeA)
    {
        _isSizeA = useSizeA;
        _boxCollider.size = useSizeA ? sizeA : sizeB;
        _boxCollider.center = useSizeA ? centerOffsetA : centerOffsetB;

        ForceClosePrompt();
    }

    private void ForceClosePrompt()
    {
        if (!_isPromptActive) return;

        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.Y);
        _isPromptActive = false;
    }

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