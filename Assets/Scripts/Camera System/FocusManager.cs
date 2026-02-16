using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
using static PauseUtils;

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
        
        // --- Parámetros de Mensaje Opcionales ---
        public string Message;      
        public Color MessageColor;  
        public float MessageDuration; 

        public Action OnComplete;
    }

    private List<FocusRequest> _pendingRequests = new();
    private bool _isCollectingRequests;
    private bool _paused;

    private const string TUTORIAL_BUTTON_NAME = "Accept";
    public string TutorialKey => TUTORIAL_BUTTON_NAME;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);

    public void OnPauseChanged(bool paused) => _paused = paused;

    // Métodos públicos con parámetros opcionales para no romper llamadas existentes
    public void RequestObjectFocus(Transform cameraPos, Transform lookAt, float duration, float zoomAmount,
        AnimationCurve zoomCurve, string message = "", Color? color = null, float msgDuration = 1.5f)
    {
        AddRequestInternal(9999, cameraPos, lookAt, duration, zoomAmount, zoomCurve, null, message, color ?? Color.white, msgDuration);
    }
    
    public void RequestRevealFocus(int orderIndex, Transform cameraPos, Transform lookAt, float duration, float zoomAmt,
        AnimationCurve curve, Action onFinishedCallback)
    {
        // En reveal focus pasamos mensaje vacío por defecto
        AddRequestInternal(orderIndex, cameraPos, lookAt, duration, zoomAmt, curve, onFinishedCallback);
    }

    public void RequestTutorial(TutorialFocusPoint point)
    {
        if (point == null) return;

        // Verificamos si el tutorial ya fue visto usando tu sistema de Save
        bool seen = Save.IsTutorialSeen(point.Id);

        if (seen)
        {
            // Si ya se vio, solo hacemos el focus de cámara sin mostrar el mensaje de nuevo
            AddRequestInternal(9999, point.CameraPos, point.LookAt, point.Time, point.ZoomAmount, point.ZoomCurve,
                null, string.Empty, Color.white, 0f);
        }
        else
        {
            // Si es la primera vez, pasamos el mensaje y marcamos como visto al terminar
            AddRequestInternal(9999, point.CameraPos, point.LookAt, point.Time, point.ZoomAmount, point.ZoomCurve,
                () => { Save.MarkTutorialSeen(point.Id); }, 
                point.Message, 
                point.TextColor, 
                point.MessageDuration);
        }
    }
    
    // Método interno unificado con parámetros opcionales al final
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

            // 1. Fase de Movimiento y Zoom
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

            // 2. Fase de Mensaje (Solo si existe un texto)
            if (!string.IsNullOrEmpty(req.Message))
            {
                GameEventManager.Instance.levelEvents.OnShowFocusMessage.Raise(req.Message, req.MessageColor);
                
                // Esperamos el tiempo de lectura respetando la pausa
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

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("FocusManager", false);
    }
}