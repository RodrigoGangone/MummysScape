using UnityEngine;

public class FocusOnActivation : MonoBehaviour
{
    [Header("Datos de foco")]
    [SerializeField] private Transform cameraFocusPos;
    [SerializeField] private Transform cameraFocusLookAt;
    [SerializeField] private float focusDuration = 2f;
    [SerializeField] private bool onlyOnce = true;

    // 1. NUEVA OPCIÓN: Define si este foco debe devolver el control o no
    [Tooltip("Si es false, el jugador se quedará bloqueado al terminar el foco (útil si luego sigue una cinemática).")]
    [SerializeField] private bool unlockOnFinish = true; 

    private bool _used;

    public void Activate()
    {
        if (onlyOnce && _used) return;

        _used = true;

        if (FocusManager.Instance != null)
        {
            // 2. Pasamos el nuevo parámetro al Manager
            FocusManager.Instance.RequestObjectFocus(
                cameraFocusPos,
                cameraFocusLookAt,
                focusDuration
            );
        }
        else
        {
            Debug.LogWarning("[FocusOnActivation] No hay FocusManager en la escena.");
        }
    }
}