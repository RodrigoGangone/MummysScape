using UnityEngine;

public class FocusOnActivation : MonoBehaviour
{
    [Header("Posición y Tiempo")]
    [SerializeField] private Transform cameraFocusPos;
    [SerializeField] private Transform cameraFocusLookAt;
    [SerializeField] private float focusDuration = 2f;
    [SerializeField] private bool onlyOnce = true;

    [Header("Estilo del Foco")]
    [SerializeField] private float zoomAmount = 3.0f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Mensaje Opcional")]
    [SerializeField, TextArea(3, 10)] private string message; // Si se deja vacío, no aparecerá nada
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float messageDuration = 2.5f;
    
    private bool _used;

    public void Activate()
    {
        if (onlyOnce && _used) return;

        _used = true;

        if (FocusManager.Instance != null)
        {
            // Ahora enviamos los parámetros de mensaje, color y duración
            FocusManager.Instance.RequestObjectFocus(
                cameraFocusPos,
                cameraFocusLookAt,
                focusDuration,
                zoomAmount,
                zoomCurve,
                message,          // Nuevo: Texto opcional
                textColor,        // Nuevo: Color de la tipografía
                messageDuration   // Nuevo: Tiempo de lectura
            );
        }
        else
        {
            Debug.LogWarning("[FocusOnActivation] No hay FocusManager.");
        }
    }
}