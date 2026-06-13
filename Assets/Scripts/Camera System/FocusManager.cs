using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;
using static PauseUtils;

/// <summary> 
/// Orquestador de Foco: Sistema centralizado que gestiona colas de peticiones para dirigir una 
/// única Virtual Camera nativa hacia objetivos, controlando parámetros de zoom, duración y mensajes. 
/// </summary>
public class FocusManager : MonoBehaviour, IPausable
{
    public static FocusManager Instance { get; private set; }

    [Header("Cámara Nativa")]
    [SerializeField, Tooltip("La Virtual Camera compartida para todos los focos.")] 
    private CinemachineVirtualCamera focusCam;
    [SerializeField] private float bufferBetweenFocus = 0.5f;

    [Header("Tutorial Replay Cancel")]
    [SerializeField] private string cancelReplayText = "Presione Y para cancelar";
    [SerializeField] private Color cancelReplayColor = Color.white;

    private class FocusRequest
    {
        public int PriorityIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public Transform LookAt;
        public float Duration;
        public float ZoomAmount;
        public AnimationCurve ZoomCurve;

        public string Message;
        public Color MessageColor;
        public float MessageDuration;

        public bool CanBeCancelled;
        public string CancelText;
        public Color CancelColor;
        public Action OnCancelled;
        public Action OnComplete;
    }

    private List<FocusRequest> _pendingRequests = new();
    private bool _isCollectingRequests;
    private bool _isSequenceRunning;
    private bool _paused;

    private const string TUTORIAL_BUTTON_NAME = "Accept";
    private const string LOCK_ID = "FocusManager";

    public string TutorialKey => TUTORIAL_BUTTON_NAME;
    public bool IsBusy => _isCollectingRequests || _pendingRequests.Count > 0 || _isSequenceRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RequestObjectFocus(
        Transform cameraPos,
        Transform lookAt,
        float duration,
        float zoomAmount,
        AnimationCurve zoomCurve,
        string message = "",
        Color? color = null,
        float msgDuration = 1.5f)
    {
        AddRequestInternal(
            index: 9999,
            camT: cameraPos,
            lookAt: lookAt,
            duration: duration,
            zoomAmt: zoomAmount,
            curve: zoomCurve,
            onComplete: null,
            message: message,
            msgColor: color ?? Color.white,
            msgDuration: msgDuration
        );
    }

    public void RequestRevealFocus(
        int orderIndex,
        Transform cameraPos,
        Transform lookAt,
        float duration,
        float zoomAmt,
        AnimationCurve curve,
        Action onFinishedCallback)
    {
        AddRequestInternal(
            index: orderIndex,
            camT: cameraPos,
            lookAt: lookAt,
            duration: duration,
            zoomAmt: zoomAmt,
            curve: curve,
            onComplete: onFinishedCallback
        );
    }

    public void RequestTutorial(TutorialFocusPoint point, Action onCancelled = null)
    {
        if (point == null) return;

        bool seen = Save.IsTutorialSeen(point.Tutorial);

        if (seen)
        {
            AddRequestInternal(
                index: 9999,
                camT: point.CameraPos,
                lookAt: point.LookAt,
                duration: point.Time,
                zoomAmt: point.ZoomAmount,
                curve: point.ZoomCurve,
                onComplete: null,
                message: string.Empty,
                msgColor: Color.white,
                msgDuration: 0f,
                canBeCancelled: true,
                cancelText: cancelReplayText,
                cancelColor: cancelReplayColor,
                onCancelled: onCancelled
            );
        }
        else
        {
            AddRequestInternal(
                index: 9999,
                camT: point.CameraPos,
                lookAt: point.LookAt,
                duration: point.Time,
                zoomAmt: point.ZoomAmount,
                curve: point.ZoomCurve,
                onComplete: () => Save.MarkTutorialSeen(point.Tutorial),
                message: point.Message,
                msgColor: point.TextColor,
                msgDuration: point.MessageDuration,
                canBeCancelled: false,
                cancelText: string.Empty,
                cancelColor: Color.white,
                onCancelled: null
            );
        }
    }

    private void AddRequestInternal(
        int index,
        Transform camT,
        Transform lookAt,
        float duration,
        float zoomAmt,
        AnimationCurve curve,
        Action onComplete,
        string message = "",
        Color? msgColor = null,
        float msgDuration = 1.5f,
        bool canBeCancelled = false,
        string cancelText = "",
        Color? cancelColor = null,
        Action onCancelled = null)
    {
        if (camT == null || focusCam == null) return;

        var req = new FocusRequest
        {
            PriorityIndex = index,
            Position = camT.position,
            Rotation = camT.rotation,
            LookAt = lookAt,
            Duration = duration,
            ZoomAmount = zoomAmt,
            ZoomCurve = curve,
            Message = message,
            MessageColor = msgColor ?? Color.white,
            MessageDuration = msgDuration,
            CanBeCancelled = canBeCancelled,
            CancelText = cancelText,
            CancelColor = cancelColor ?? Color.white,
            OnCancelled = onCancelled,
            OnComplete = onComplete
        };

        _pendingRequests.Add(req);

        if (!_isCollectingRequests)
        {
            _isCollectingRequests = true;
            StartCoroutine(CollectAndSortRoutine());
        }
    }

    private IEnumerator CollectAndSortRoutine()
    {
        yield return new WaitForEndOfFrame();

        _pendingRequests = _pendingRequests.OrderBy(x => x.PriorityIndex).ToList();

        yield return StartCoroutine(PlaySequenceRoutine());

        _isCollectingRequests = false;
    }

    private IEnumerator PlaySequenceRoutine()
    {
        _isSequenceRunning = true;
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, true);

        float originalFOV = focusCam.m_Lens.FieldOfView;

        while (_pendingRequests.Count > 0)
        {
            FocusRequest req = _pendingRequests[0];
            _pendingRequests.RemoveAt(0);

            // Preparar la cámara antes de activarla
            focusCam.transform.position = req.Position;

            if (req.LookAt != null)
                focusCam.transform.LookAt(req.LookAt);
            else
                focusCam.transform.rotation = req.Rotation;

            focusCam.LookAt = req.LookAt;
            focusCam.m_Lens.FieldOfView = originalFOV;
            
            // Truco para evitar tirones desde la posición del foco anterior
            focusCam.PreviousStateIsValid = false;
            focusCam.Priority = 100;

            bool cancelled = false;
            float elapsed = 0f;
            float targetFOV = originalFOV - req.ZoomAmount;

            if (req.CanBeCancelled && !string.IsNullOrEmpty(req.CancelText))
            {
                GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                    ContextUIFactory.Prompt(ContextMessageType.CancelReplay, ButtonType.Y, req.CancelColor)
                );
            }

            while (elapsed < req.Duration)
            {
                if (_paused)
                {
                    yield return null;
                    continue;
                }

                if (req.CanBeCancelled && Input.GetButtonDown(TutorialKey))
                {
                    cancelled = true;
                    break;
                }

                elapsed += Time.deltaTime;

                float t = elapsed / req.Duration;
                float curveValue = req.ZoomCurve.Evaluate(t);
                focusCam.m_Lens.FieldOfView = Mathf.Lerp(originalFOV, targetFOV, curveValue);

                yield return null;
            }

            if (req.CanBeCancelled)
            {
                GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                    ContextUIFactory.Hidden()
                );
            }

            if (cancelled)
            {
                focusCam.Priority = 0;
                req.OnCancelled?.Invoke();
                continue;
            }

            focusCam.m_Lens.FieldOfView = targetFOV;

            if (!string.IsNullOrEmpty(req.Message))
            {
                GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(ContextUIFactory.CustomMessage(req.Message, req.MessageColor));

                yield return WaitForSecondsPausable(req.MessageDuration, () => _paused);

                GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                    ContextUIFactory.Hidden()
                );
            }

            req.OnComplete?.Invoke();

            focusCam.Priority = 0; // Devolver a la cámara del jugador

            if (_pendingRequests.Count > 0)
                yield return WaitForSecondsPausable(bufferBetweenFocus, () => _paused);
        }

        focusCam.m_Lens.FieldOfView = originalFOV;
        focusCam.Priority = 0;
        focusCam.LookAt = null;

        _isSequenceRunning = false;
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LOCK_ID, false);
    }

    public void OnPauseChanged(bool paused) 
    {
        _paused = paused;
        CinemachineCore.UniformDeltaTimeOverride = paused ? 0f : -1f;
    }

    private void OnEnable() =>
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);

    private void OnDisable() =>
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}