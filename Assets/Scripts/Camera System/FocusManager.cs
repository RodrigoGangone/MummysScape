using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class FocusManager : MonoBehaviour
{
    public static FocusManager Instance { get; private set; }

    [Header("Cámara de Foco")]
    [Tooltip("Asegúrate de que esta cámara tenga Priority = 0 en el Inspector por defecto")]
    [SerializeField] private CinemachineVirtualCamera focusCam;

    // ELIMINADO: [SerializeField] private DollyPositionManager dollyCameraManager;

    [Header("Input de tutorial")]
    private const string TUTORIAL_BUTTON_NAME = "Accept";
    public string TutorialKey => TUTORIAL_BUTTON_NAME;

    private bool _isFocusing;
    private Coroutine _focusRoutine;

    private readonly Dictionary<string, bool> _tutorialLearned = new Dictionary<string, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ------------------------------------------------------------------
    //  API PUBLICA (Sin cambios, compatible con tu código existente)
    // ------------------------------------------------------------------

    public void RequestObjectFocus(Transform cameraPos, Transform lookAt, float duration)
    {
        if (_isFocusing) return;
        if (cameraPos == null || lookAt == null) return;

        _focusRoutine = StartCoroutine(FocusRoutine(cameraPos, lookAt, duration, null, false));
    }

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

    // ------------------------------------------------------------------
    //  RUTINA CORE (Modificada para Priority System)
    // ------------------------------------------------------------------

    private IEnumerator FocusRoutine(
        Transform cameraPos,
        Transform lookAt,
        float duration,
        TutorialFocusPoint tutorialPoint,
        bool markLearned)
    {
        _isFocusing = true;
        
        // 1. Bloqueamos al jugador (Input) para que no camine a otras zonas
        Locked();

        // 2. Configuramos la cámara "Fantasma" (FocusCam)
        focusCam.transform.position = cameraPos.position;
        focusCam.transform.rotation = cameraPos.rotation; 
        focusCam.LookAt = lookAt; 
        // focusCam.Follow = lookAt; // Descomentar si el objeto objetivo se mueve

        // 3. ACTIVAR FOCO (Priority Override)
        // Al poner 100, Cinemachine ignora cualquier cámara de las islas (que tendrán 10 o 20)
        // y hace un blend suave hacia esta cámara.
        focusCam.Priority = 100;

        yield return new WaitForSeconds(duration);

        // 4. DESACTIVAR FOCO (Restaurar)
        // Al bajar a 0, Cinemachine busca automáticamente la siguiente cámara más alta
        // (que será la VCam de la isla donde esté parado el jugador).
        focusCam.Priority = 0;

        focusCam.Follow = null;
        focusCam.LookAt = null;

        // 5. Marcar tutorial como aprendido si corresponde
        if (tutorialPoint != null && markLearned)
        {
            string id = tutorialPoint.Id;
            if (!string.IsNullOrEmpty(id))
                _tutorialLearned[id] = true;
        }

        // 6. Desbloqueamos al jugador
        UnLocked();
        
        _isFocusing = false;
        _focusRoutine = null;
    }

    // Asumo que estos métodos se comunican con tu sistema de eventos global
    public void Locked() => GameEventManager.Instance.playerEvents.OnLocked.Raise(true);
    public void UnLocked() => GameEventManager.Instance.playerEvents.OnLocked.Raise(false);
}