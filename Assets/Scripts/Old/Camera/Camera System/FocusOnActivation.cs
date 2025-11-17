using UnityEngine;

public class FocusOnActivation : MonoBehaviour
{
    [Header("Datos de foco")]
    [SerializeField] private Transform cameraFocusPos;
    [SerializeField] private Transform cameraFocusLookAt;
    [SerializeField] private float focusDuration = 2f;
    [SerializeField] private bool onlyOnce = true;

    private bool _used;

    // Llamado por la lógica del objeto (palanca, botón, etc.)
    public void Activate()
    {
        if (onlyOnce && _used) return;

        _used = true;

        if (FocusManager.Instance != null)
        {
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