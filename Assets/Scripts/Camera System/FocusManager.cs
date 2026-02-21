using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
using static PauseUtils;

/// <summary> 
/// Orquestador de Foco: Sistema centralizado que gestiona colas de peticiones para dirigir la cámara hacia 
/// objetivos, controlando parámetros de zoom, duración y mensajes de interfaz asociados. 
/// </summary>
public class FocusManager : MonoBehaviour, IPausable
{
    public static FocusManager Instance { get; private set; }

    [SerializeField] private CinemachineVirtualCamera focusCam;
    [SerializeField] private float bufferBetweenFocus = 0.5f;

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

        public Action OnComplete;
    }

    private List<FocusRequest> _pendingRequests = new();
    private bool _isCollectingRequests;
    private bool _isSequenceRunning;
    private bool _paused;

    private const string TUTORIAL_BUTTON_NAME = "Accept";
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

    public void RequestObjectFocus(Transform cameraPos, Transform lookAt, float duration, float zoomAmount,
        AnimationCurve zoomCurve, string message = "", Color? color = null, float msgDuration = 1.5f)
    {
        AddRequestInternal(9999, cameraPos, lookAt, duration, zoomAmount, zoomCurve, null, message,
            color ?? Color.white, msgDuration);
    }

    public void RequestRevealFocus(int orderIndex, Transform cameraPos, Transform lookAt, float duration, float zoomAmt,
        AnimationCurve curve, Action onFinishedCallback)
    {
        AddRequestInternal(orderIndex, cameraPos, lookAt, duration, zoomAmt, curve, onFinishedCallback);
    }

    public void RequestTutorial(TutorialFocusPoint point)
    {
        if (point == null) return;

        bool seen = Save.IsTutorialSeen(point.Id);

        if (seen)
        {
            AddRequestInternal(9999, point.CameraPos, point.LookAt, point.Time, point.ZoomAmount, point.ZoomCurve,
                null, string.Empty, Color.white, 0f);
        }
        else
        {
            AddRequestInternal(9999, point.CameraPos, point.LookAt, point.Time, point.ZoomAmount, point.ZoomCurve,
                () => { Save.MarkTutorialSeen(point.Id); },
                point.Message,
                point.TextColor,
                point.MessageDuration);
        }
    }

    private void AddRequestInternal(int index, Transform camT, Transform lookAt, float duration, float zoomAmt,
        AnimationCurve curve, Action onComplete, string message = "", Color? msgColor = null, float msgDuration = 1.5f)
    {
        if (camT == null) return;

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
            OnComplete = onComplete,
        };

        _pendingRequests.Add(req);

        if (!_isCollectingRequests)
            StartCoroutine(CollectAndSortRoutine());
    }

    private IEnumerator CollectAndSortRoutine()
    {
        _isCollectingRequests = true;
        yield return new WaitForEndOfFrame();
        _pendingRequests = _pendingRequests.OrderBy(x => x.PriorityIndex).ToList();
        yield return StartCoroutine(PlaySequenceRoutine());
        _isCollectingRequests = false;
    }

    private IEnumerator PlaySequenceRoutine()
    {
        _isSequenceRunning = true;
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("FocusManager", true);

        float originalFOV = focusCam.m_Lens.FieldOfView;

        while (_pendingRequests.Count > 0)
        {
            FocusRequest req = _pendingRequests[0];
            _pendingRequests.RemoveAt(0);

            focusCam.transform.position = req.Position;
            if (req.LookAt != null) focusCam.transform.LookAt(req.LookAt);
            else focusCam.transform.rotation = req.Rotation;

            focusCam.LookAt = req.LookAt;
            focusCam.PreviousStateIsValid = false;
            focusCam.m_Lens.FieldOfView = originalFOV;
            focusCam.Priority = 100;

            float elapsed = 0f;
            float targetFOV = originalFOV - req.ZoomAmount;

            while (elapsed < req.Duration)
            {
                if (_paused)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / req.Duration;
                float curveValue = req.ZoomCurve.Evaluate(t);
                focusCam.m_Lens.FieldOfView = Mathf.Lerp(originalFOV, targetFOV, curveValue);
                yield return null;
            }

            focusCam.m_Lens.FieldOfView = targetFOV;

            if (!string.IsNullOrEmpty(req.Message))
            {
                GameEventManager.Instance.levelEvents.OnShowFocusMessage.Raise(req.Message, req.MessageColor);
                yield return WaitForSecondsPausable(req.MessageDuration, () => _paused);
                GameEventManager.Instance.levelEvents.OnHideFocusMessage.Raise();
            }

            req.OnComplete?.Invoke();

            if (_pendingRequests.Count > 0)
                yield return WaitForSecondsPausable(bufferBetweenFocus, () => _paused);
        }

        focusCam.m_Lens.FieldOfView = originalFOV;
        focusCam.Priority = 0;
        focusCam.LookAt = null;

        _isSequenceRunning = false;
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("FocusManager", false);
    }

    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}