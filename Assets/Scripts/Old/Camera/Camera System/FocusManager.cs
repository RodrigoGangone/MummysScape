using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class FocusManager : MonoBehaviour
{
    public static FocusManager Instance { get; private set; }

    [Header("Cámaras")]
    // ELIMINAMOS: [SerializeField] private CinemachineVirtualCamera gameplayCam;
    [SerializeField] private CinemachineVirtualCamera focusCam;

    [Header("Integración con Dolly")]
    // CAMBIAMOS EL TIPO Y NOMBRE DE LA VARIABLE
    [SerializeField] private DollyPositionManager dollyCameraManager;

    [Header("Input de tutorial opcional (clave global)")]
    [SerializeField] private KeyCode tutorialKey = KeyCode.T;
    public KeyCode TutorialKey => tutorialKey;

    private bool _isFocusing; 
    private Coroutine _focusRoutine;

    private readonly Dictionary<string, bool> _tutorialLearned =
        new Dictionary<string, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ... (Todos los métodos Request... quedan EXACTAMENTE IGUAL) ...

    
    
    // -------------------- Focus objetos normales --------------------
    public void RequestObjectFocus(Transform cameraPos, Transform lookAt, float duration)
    {
        if (_isFocusing) return;
        if (cameraPos == null || lookAt == null) return;

        _focusRoutine = StartCoroutine(FocusRoutine(cameraPos, lookAt, duration, null, false));
    }

    // ----------------------- Tutorial: primera vez ------------------
    public void RequestTutorialFirstTime(TutorialFocusPoint point)
    {
        if (point == null) return;
        string id = point.Id;
        if (string.IsNullOrEmpty(id)) return;

        if (_tutorialLearned.TryGetValue(id, out bool alreadyLearned) && alreadyLearned)
            return;

        if (_isFocusing) return;

        _focusRoutine = StartCoroutine(FocusRoutine(
            point.CameraPos,
            point.LookAt,
            point.MandatoryTime,
            point,
            markLearned: true
        ));
    }

    // ----------------------- Tutorial: opcional ---------------------
    public void RequestTutorialOptional(TutorialFocusPoint point)
    {
        if (point == null) return;
        string id = point.Id;
        if (string.IsNullOrEmpty(id)) return;

        if (!_tutorialLearned.TryGetValue(id, out bool learned) || !learned)
        {
            RequestTutorialFirstTime(point);
            return;
        }

        if (_isFocusing) return;

        _focusRoutine = StartCoroutine(FocusRoutine(
            point.CameraPos,
            point.LookAt,
            point.OptionalTime,
            point,
            markLearned: false
        ));
    }


    // ---------------------- Rutina central (MODIFICADA) -----------------
    private IEnumerator FocusRoutine(
        Transform cameraPos,
        Transform lookAt,
        float duration,
        TutorialFocusPoint tutorialPoint,
        bool markLearned)
    {
        _isFocusing = true;

        // 1. Bloquea el movimiento de zonas del Dolly
        if (dollyCameraManager != null)
            dollyCameraManager.SetZonesLocked(true);

        // 2. Configura la cámara de foco
        focusCam.Follow = lookAt;
        focusCam.LookAt = lookAt;
        focusCam.transform.position = cameraPos.position;

        // 3. APAGA la cámara de gameplay activa (le da prioridad 0)
        if (dollyCameraManager != null)
            dollyCameraManager.ActivateGameplayCamera(false);
        
        // 4. ENCIENDE la cámara de foco
        focusCam.Priority = 10;

        yield return new WaitForSeconds(duration);

        // 5. DEVUELVE la prioridad a la cámara de gameplay
        if (dollyCameraManager != null)
            dollyCameraManager.ActivateGameplayCamera(true);

        // 6. APAGA la cámara de foco
        focusCam.Priority = 0;

        focusCam.Follow = null;
        focusCam.LookAt = null;

        // 7. Marca el tutorial si es necesario
        if (tutorialPoint != null && markLearned)
        {
            string id = tutorialPoint.Id;
            if (!string.IsNullOrEmpty(id))
                _tutorialLearned[id] = true;
        }

        // 8. Desbloquea el movimiento de zonas
        if (dollyCameraManager != null)
            dollyCameraManager.SetZonesLocked(false);

        _isFocusing = false;
        _focusRoutine = null;
    }
}