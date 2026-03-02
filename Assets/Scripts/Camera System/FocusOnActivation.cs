using UnityEngine;

/// <summary> 
/// Disparador de Foco: Componente que solicita una secuencia de enfoque al FocusManager de forma 
/// programática al activarse, ideal para resaltar cambios en el entorno tras una acción. 
/// </summary>
public class FocusOnActivation : MonoBehaviour
{
    [Header("Posición y Tiempo")] [SerializeField]
    private Transform cameraFocusPos;

    [SerializeField] private Transform cameraFocusLookAt;
    [SerializeField] private float focusDuration = 2f;
    [SerializeField] private bool onlyOnce = true;

    [Header("Estilo del Foco")] [SerializeField]
    private float zoomAmount = 3.0f;

    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Mensaje Opcional")] [SerializeField, TextArea(3, 10)]
    private string message;

    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float messageDuration = 2.5f;

    private bool _used;

    public void Activate()
    {
        if (onlyOnce && _used) return;

        _used = true;

        if (FocusManager.Instance != null)
        {
            FocusManager.Instance.RequestObjectFocus(
                cameraFocusPos,
                cameraFocusLookAt,
                focusDuration,
                zoomAmount,
                zoomCurve,
                message,
                textColor,
                messageDuration
            );
        }
        else
        {
            Debug.LogWarning("[FocusOnActivation] No hay FocusManager.");
        }
    }
}