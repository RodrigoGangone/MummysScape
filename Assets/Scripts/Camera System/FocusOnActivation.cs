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
                zoomCurve  
            );
        }
        else
        {
            Debug.LogWarning("[FocusOnActivation] No hay FocusManager.");
        }
    }
}