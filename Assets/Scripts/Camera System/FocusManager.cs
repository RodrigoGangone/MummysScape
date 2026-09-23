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
    public const string LockId = "FocusManager";

    /// <summary>Una activación cancelable; finalizar siempre libera los permisos de su grupo.</summary>
    public sealed class ActivationHandle : IDisposable
    {
        private readonly MonoBehaviour _owner;
        private Action<bool> _onReady;
        private Action _onFinished;
        private Func<bool> _isPreparing;
        public bool IsFinished { get; private set; }
        public bool HasArrived { get; private set; }
        internal bool OwnerIsActive => _owner != null && _owner.isActiveAndEnabled;

        internal ActivationHandle(MonoBehaviour owner, Action<bool> onReady, Action onFinished, Func<bool> isPreparing)
        {
            _owner = owner;
            _onReady = onReady;
            _onFinished = onFinished;
            _isPreparing = isPreparing;
        }

        internal bool IsPreparing => !IsFinished && (_isPreparing?.Invoke() ?? false);

        internal void Arrive(bool hasFocus)
        {
            if (IsFinished || HasArrived) return;
            if (!OwnerIsActive) { Dispose(); return; }
            HasArrived = true;
            var callback = _onReady;
            _onReady = null;
            callback?.Invoke(hasFocus);
        }

        public void Dispose()
        {
            if (IsFinished) return;
            IsFinished = true;
            _onReady = null;
            var callback = _onFinished;
            _onFinished = null;
            _isPreparing = null;
            callback?.Invoke();
        }
    }

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
        public float BlendInDuration;
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
        public ActivationHandle Activation;
        public bool CameraArrived;
    }

    private List<FocusRequest> _pendingRequests = new();
    private bool _isCollectingRequests;
    private bool _isSequenceRunning;
    private bool _paused;
    private float _activeBlendInDuration = -1f;
    private FocusRequest _activeRequest;
    private float _originalFOV;

    private const string TUTORIAL_BUTTON_NAME = "Accept";

    public string TutorialKey => TUTORIAL_BUTTON_NAME;
    public bool IsBusy => _isCollectingRequests || _pendingRequests.Count > 0 || _isSequenceRunning;
    public bool CanFocus => isActiveAndEnabled && focusCam != null && focusCam.isActiveAndEnabled &&
        CinemachineCore.Instance.FindPotentialTargetBrain(focusCam) != null;

    public ActivationHandle RequestActivationFocus(MonoBehaviour owner, Transform cameraPos,
        Transform lookAt, float duration, float zoomAmount, AnimationCurve zoomCurve,
        Action<bool> onReady, Action onFinished = null, string message = "",
        Color? color = null, float msgDuration = 1.5f, float blendInDuration = -1f,
        Func<bool> isPreparing = null)
    {
        if (!CanFocus || cameraPos == null || owner == null || !owner.isActiveAndEnabled) return null;
        var handle = new ActivationHandle(owner, onReady, onFinished, isPreparing);
        AddRequestInternal(9999, cameraPos, lookAt, duration, zoomAmount, zoomCurve, null,
            message, color, msgDuration, blendInDuration: blendInDuration, activation: handle);
        return handle;
    }

    private void HandleCameraUpdated(CinemachineBrain brain)
    {
        if (_activeRequest?.Activation == null || _paused) return;
        if (brain == CinemachineCore.Instance.FindPotentialTargetBrain(focusCam) &&
            ReferenceEquals(brain.ActiveVirtualCamera, focusCam) && !brain.IsBlending)
            _activeRequest.CameraArrived = true;
    }

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
        float msgDuration = 1.5f,
        float blendInDuration = -1f)
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
            msgDuration: msgDuration,
            blendInDuration: blendInDuration
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
        Action onCancelled = null,
        float blendInDuration = -1f,
        ActivationHandle activation = null)
    {
        if (camT == null || focusCam == null) return;

        var req = new FocusRequest
        {
            PriorityIndex = index,
            Position = camT.position,
            Rotation = camT.rotation,
            LookAt = lookAt,
            Duration = duration,
            BlendInDuration = blendInDuration,
            ZoomAmount = zoomAmt,
            ZoomCurve = curve,
            Message = message,
            MessageColor = msgColor ?? Color.white,
            MessageDuration = msgDuration,
            CanBeCancelled = canBeCancelled,
            CancelText = cancelText,
            CancelColor = cancelColor ?? Color.white,
            OnCancelled = onCancelled,
            OnComplete = onComplete,
            Activation = activation
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
        // También funciona en batchmode, donde WaitForEndOfFrame no se reanuda.
        yield return null;

        _pendingRequests = _pendingRequests.OrderBy(x => x.PriorityIndex).ToList();

        yield return StartCoroutine(PlaySequenceRoutine());

        _isCollectingRequests = false;
    }

    private IEnumerator PlaySequenceRoutine()
    {
        _isSequenceRunning = true;
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LockId, true);

        float originalFOV = focusCam != null ? focusCam.m_Lens.FieldOfView : 60f;
        _originalFOV = originalFOV;

        while (_pendingRequests.Count > 0)
        {
            FocusRequest req = _pendingRequests[0];
            _pendingRequests.RemoveAt(0);
            _activeRequest = req;
            if (ActivationWasCancelled(req)) continue;
            if (req.Activation != null && !CanFocus)
            {
                ActivateWithoutCamera(req);
                continue;
            }
            if (focusCam == null) continue;

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
            _activeBlendInDuration = req.BlendInDuration;
            focusCam.Priority = 100;

            if (req.Activation != null)
            {
                while (!req.CameraArrived && !ActivationWasCancelled(req) && CanFocus)
                    yield return null;

                if (ActivationWasCancelled(req))
                {
                    focusCam.Priority = 0;
                    yield return null;
                    continue;
                }

                if (!CanFocus)
                {
                    ActivateWithoutCamera(req);
                    if (focusCam != null) focusCam.Priority = 0;
                    continue;
                }

                while (_paused && !ActivationWasCancelled(req)) yield return null;
                req.Activation.Arrive(true);
                // Las lanzas reciben el nuevo peso en LateUpdate. Dejar que inicien sus efectos.
                yield return null;
                while (!ActivationWasCancelled(req) && CanFocus && (_paused || req.Activation.IsPreparing))
                    yield return null;
                if (ActivationWasCancelled(req))
                {
                    if (focusCam != null) focusCam.Priority = 0;
                    yield return null;
                    continue;
                }
            }

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
                if (ActivationWasCancelled(req)) { cancelled = true; break; }
                if (req.Activation != null && !CanFocus)
                {
                    req.Activation.Dispose();
                    cancelled = true;
                    break;
                }
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
                float curveValue = req.ZoomCurve != null ? req.ZoomCurve.Evaluate(t) : t;
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
                if (focusCam != null) focusCam.Priority = 0;
                req.OnCancelled?.Invoke();
                if (req.Activation != null) yield return null;
                continue;
            }

            focusCam.m_Lens.FieldOfView = targetFOV;

            if (!string.IsNullOrEmpty(req.Message))
            {
                GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(ContextUIFactory.CustomMessage(req.Message, req.MessageColor));

                float messageElapsed = 0f;
                while (messageElapsed < req.MessageDuration && !ActivationWasCancelled(req))
                {
                    if (!_paused) messageElapsed += Time.deltaTime;
                    yield return null;
                }

                GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                    ContextUIFactory.Hidden()
                );
            }

            req.OnComplete?.Invoke();
            req.Activation?.Dispose();
            _activeRequest = null;

            focusCam.Priority = 0; // Devolver a la cámara del jugador

            // Permite que el Brain procese la salida antes de reutilizar la misma cámara.
            if (req.Activation != null) yield return null;
            if (_pendingRequests.Count > 0)
                yield return WaitForSecondsPausable(bufferBetweenFocus, () => _paused);
        }

        if (focusCam != null)
        {
            focusCam.m_Lens.FieldOfView = originalFOV;
            focusCam.Priority = 0;
            focusCam.LookAt = null;
        }

        _isSequenceRunning = false;
        _activeBlendInDuration = -1f;
        _activeRequest = null;
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LockId, false);
    }

    private static bool ActivationWasCancelled(FocusRequest request)
    {
        if (request.Activation == null) return false;
        if (!request.Activation.OwnerIsActive) request.Activation.Dispose();
        return request.Activation.IsFinished;
    }

    private void ActivateWithoutCamera(FocusRequest request)
    {
        Debug.LogWarning("[FocusManager] Se perdió la cámara; activando sin foco.", this);
        request.Activation.Arrive(false);
        request.Activation.Dispose();
    }

    private CinemachineBlendDefinition OverrideFocusBlend(
        ICinemachineCamera fromCamera, ICinemachineCamera toCamera,
        CinemachineBlendDefinition defaultBlend, MonoBehaviour owner)
    {
        // Solo cambia la entrada al foco solicitado; el regreso usa el blend habitual.
        if (ReferenceEquals(toCamera, focusCam) && _activeBlendInDuration >= 0f)
            return new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.EaseInOut, _activeBlendInDuration);

        return defaultBlend;
    }

    public void OnPauseChanged(bool paused) 
    {
        _paused = paused;
        CinemachineCore.UniformDeltaTimeOverride = paused ? 0f : -1f;
    }

    private void OnEnable()
    {
        CinemachineCore.GetBlendOverride += OverrideFocusBlend;
        CinemachineCore.CameraUpdatedEvent.AddListener(HandleCameraUpdated);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    }

    private void OnDisable()
    {
        CinemachineCore.GetBlendOverride -= OverrideFocusBlend;
        CinemachineCore.CameraUpdatedEvent.RemoveListener(HandleCameraUpdated);
        StopAllCoroutines();
        _activeRequest?.Activation?.Dispose();
        foreach (var request in _pendingRequests.ToArray()) request.Activation?.Dispose();
        _pendingRequests.Clear();
        if (focusCam != null)
        {
            if (_isSequenceRunning) focusCam.m_Lens.FieldOfView = _originalFOV;
            focusCam.Priority = 0;
            focusCam.LookAt = null;
        }
        _activeRequest = null;
        _isCollectingRequests = false;
        _isSequenceRunning = false;
        _activeBlendInDuration = -1f;
        CinemachineCore.UniformDeltaTimeOverride = -1f;
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.playerEvents.OnLockRequested.Raise(LockId, false);
            GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
