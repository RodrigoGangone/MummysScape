using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using UnityEngine.Video;
using static PauseUtils;

[RequireComponent(typeof(BoxCollider))]
public class TutorialTrigger : MonoBehaviour, IPausable
{
    [Header("Referencias")] 
    [SerializeField] private TutorialFocusPoint focusPoint;
    [SerializeField] private ParticleSystem[] braziers;
    private VideoPlayer _tutorialVideo;

    [Header("Configuración de Áreas")] 
    [SerializeField] private Vector3 sizeA = Vector3.one;
    [SerializeField] private Vector3 sizeB = Vector3.one * 2f;
    [SerializeField] private Vector3 centerOffsetA = Vector3.zero;
    [SerializeField] private Vector3 centerOffsetB = Vector3.zero;

    [Header("Audio")]
    [SerializeField] private FxBank _bank;
    [SerializeField] private string keySound;
    
    private BoxCollider _boxCollider;
    private bool _isPromptActive;
    private Coroutine _effectRoutine;
    private bool _paused;
    private bool _isPlaying;
    
    // Referencia directa al estado de guardado
    private bool IsTutorialAlreadySeen => Save.IsTutorialSeen(focusPoint.Id);
    
    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _tutorialVideo = GetComponentInChildren<VideoPlayer>();

        // Ajustar el colisionador según si ya se completó el tutorial
        SetColliderShape(!IsTutorialAlreadySeen);
        ToggleEffects(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather") || IsTutorialAlreadySeen) return;

        // Si es la primera vez, lanzamos la secuencia completa
        ExecuteTutorialSequence();
        SetColliderShape(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("PlayerFather") || !IsTutorialAlreadySeen) return;

        if (!_isPromptActive)
        {
            GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.Y);
            _isPromptActive = true;
        }

        // Re-visualización del tutorial (sin mensaje, según lógica de FocusManager)
        if (!_paused && !_isPlaying && Input.GetButtonDown(FocusManager.Instance.TutorialKey))
        {
            ExecuteTutorialSequence();
            _isPlaying = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.Y);
        _isPromptActive = false;
    }

    private void ExecuteTutorialSequence()
    {
        if (focusPoint == null) return;

        _bank.Play2D(keySound);
        
        // Enviamos la petición al FocusManager que ahora maneja los mensajes opcionales
        FocusManager.Instance.RequestTutorial(focusPoint);

        if (_effectRoutine != null) StopCoroutine(_effectRoutine);

        // CÁLCULO DE TIEMPO DINÁMICO:
        // Si no se ha visto, sumamos el tiempo de cámara + duración del mensaje.
        // Si ya se vio, solo usamos el tiempo de la cámara.
        float totalDuration = focusPoint.Time;
        if (!IsTutorialAlreadySeen && !string.IsNullOrEmpty(focusPoint.Message))
        {
            totalDuration += focusPoint.MessageDuration;
        }

        _effectRoutine = StartCoroutine(TutorialDurationRoutine(totalDuration));
    }

    private IEnumerator TutorialDurationRoutine(float duration)
    {
        ToggleEffects(true);

        // Espera pausabe que respeta el estado del juego
        yield return WaitForSecondsPausable(duration, () => _paused);

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
        _boxCollider.size = useSizeA ? sizeA : sizeB;
        _boxCollider.center = useSizeA ? centerOffsetA : centerOffsetB;
    }

    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

    #region Gizmo
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