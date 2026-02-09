using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
// Agregamos esto para usar WaitForSecondsPausable fácilmente
using static PauseUtils;

public class FocusManager : MonoBehaviour, IPausable // 1. Implementamos la interfaz
{
    public static FocusManager Instance { get; private set; }

    [Header("Cámara de Foco")] [SerializeField]
    private CinemachineVirtualCamera focusCam;

    [Header("Configuración")] [SerializeField]
    private float bufferBetweenFocus = 0.5f;

    [Header("Efecto Zoom (Push-In)")]
    [Tooltip("Cuánto zoom hace la cámara durante el foco (grados de FOV).")]
    [SerializeField]
    private float zoomAmount = 3.0f;

    [Tooltip("Curva para suavizar el movimiento del zoom.")] [SerializeField]
    private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private class FocusRequest
    {
        public int PriorityIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public Transform LookAt;
        public float Duration;
        public Action OnComplete;

        // AGREGAR ESTO: Por defecto true para que todo lo viejo siga funcionando
        public bool UnlockOnFinish = true;
    }

    private List<FocusRequest> _pendingRequests = new();
    private bool _isCollectingRequests;

    // Variable para controlar el estado de pausa localmente
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

// Agregamos el parámetro opcional al final
    public void RequestObjectFocus(Transform cameraPos, Transform lookAt, float duration, bool unlockPlayer = true)
    {
        // Pasamos 'unlockPlayer' al método interno
        AddRequestInternal(9999, cameraPos, lookAt, duration, null, unlockPlayer);
    }

    public void RequestRevealFocus(int orderIndex, Transform cameraPos, Transform lookAt, float duration,
        Action onFinishedCallback)
    {
        AddRequestInternal(orderIndex, cameraPos, lookAt, duration, onFinishedCallback);
    }

    public void RequestTutorial(TutorialFocusPoint point)
    {
        if (point == null) return;
        bool seen = Save.IsTutorialSeen(point.Id);

        if (seen)
            AddRequestInternal(9999, point.CameraPos, point.LookAt, point.Time, null);
        else
            AddRequestInternal(9999, point.CameraPos, point.LookAt, point.Time,
                () => { Save.MarkTutorialSeen(point.Id); });
    }

// Modificamos la firma para aceptar el bool (por defecto true)
    private void AddRequestInternal(int index, Transform camT, Transform lookAt, float duration, Action onComplete,
        bool unlockOnFinish = true)
    {
        if (camT == null) return;

        var req = new FocusRequest
        {
            PriorityIndex = index,
            Position = camT.position,
            Rotation = camT.rotation,
            LookAt = lookAt,
            Duration = duration,
            OnComplete = onComplete,
            UnlockOnFinish = unlockOnFinish // <--- ASIGNAMOS EL VALOR
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
        // 1. Bloqueamos siempre al empezar cualquier secuencia de focos
        GameEventManager.Instance.playerEvents.OnLocked.Raise(true);

        float originalFOV = focusCam.m_Lens.FieldOfView;

        // Variable local para recordar la preferencia del último request.
        // Lo iniciamos en true por seguridad.
        bool shouldUnlockAtEnd = true;

        while (_pendingRequests.Count > 0)
        {
            FocusRequest req = _pendingRequests[0];
            _pendingRequests.RemoveAt(0);

            // ---> AQUI ESTA EL CAMBIO <---
            // Actualizamos la variable con la preferencia de ESTA petición.
            // Como es un bucle, al final nos quedaremos con el valor de la última petición de la cola.
            shouldUnlockAtEnd = req.UnlockOnFinish;

            // --- Lógica de movimiento de cámara (sin cambios) ---
            focusCam.transform.position = req.Position;

            if (req.LookAt != null)
                focusCam.transform.LookAt(req.LookAt);
            else
                focusCam.transform.rotation = req.Rotation;

            focusCam.LookAt = req.LookAt;
            focusCam.PreviousStateIsValid = false;
            focusCam.m_Lens.FieldOfView = originalFOV;
            focusCam.Priority = 100;

            float elapsed = 0f;
            float targetFOV = originalFOV - zoomAmount;

            while (elapsed < req.Duration)
            {
                if (_paused)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / req.Duration;

                float curveValue = zoomCurve.Evaluate(t);
                focusCam.m_Lens.FieldOfView = Mathf.Lerp(originalFOV, targetFOV, curveValue);

                yield return null;
            }

            focusCam.m_Lens.FieldOfView = targetFOV;

            // Ejecutamos callbacks (Save tutorial, etc.)
            req.OnComplete?.Invoke();

            if (_pendingRequests.Count > 0)
                yield return WaitForSecondsPausable(bufferBetweenFocus, () => _paused);
        }

        // Restaurar cámara
        focusCam.m_Lens.FieldOfView = originalFOV;
        focusCam.Priority = 0;
        focusCam.LookAt = null;

        // ---> LA VALIDACIÓN FINAL <---
        // Solo quitamos el candado si el último foco tenía 'UnlockOnFinish = true'.
        // Si fue el del Sarcófago (que le pasaste false), esto se saltará
        // y el Player seguirá bloqueado hasta que tu Timeline lo libere.
        if (shouldUnlockAtEnd)
        {
            GameEventManager.Instance.playerEvents.OnLocked.Raise(false);
        }
    }
}