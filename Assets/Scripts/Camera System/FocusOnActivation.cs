using System;
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
    [Tooltip("Duración del paneo de entrada. -1 conserva la transición de la cámara.")]
    [SerializeField] private float blendInDuration = -1f;
    [SerializeField] private bool onlyOnce = true;

    [Header("Estilo del Foco")] [SerializeField]
    private float zoomAmount = 3.0f;

    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Mensaje Opcional")] [SerializeField, TextArea(3, 10)]
    private string message;

    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float messageDuration = 2.5f;

    private bool _used;
    private FocusManager.ActivationHandle _activation;

    public bool CanFocus => isActiveAndEnabled && cameraFocusPos != null &&
        FocusManager.Instance != null && FocusManager.Instance.CanFocus;
    public bool IsPending => _activation != null && !_activation.IsFinished;

    /// <summary>
    /// Ejecuta una sola acción al llegar. El bool indica si hay un foco propio activo;
    /// sin cámara o si onlyOnce ya se consumió, ejecuta inmediatamente con false.
    /// El propietario permite cancelar aunque el foco esté en otro objeto del grupo.
    /// </summary>
    public FocusManager.ActivationHandle ActivateWhenFocused(MonoBehaviour owner,
        Action<bool> onReady, Action onFinished = null, Func<bool> isPreparing = null)
    {
        if (owner == null || !owner.isActiveAndEnabled) return null;
        if (IsPending) return _activation;
        if (onlyOnce && _used)
        {
            onReady?.Invoke(false);
            onFinished?.Invoke();
            return null;
        }

        _used = true;
        if (CanFocus)
        {
            _activation = FocusManager.Instance.RequestActivationFocus(owner, cameraFocusPos,
                cameraFocusLookAt, focusDuration, zoomAmount, zoomCurve, onReady, onFinished,
                message, textColor, messageDuration, blendInDuration, isPreparing);
            if (_activation != null) return _activation;
        }

        Debug.LogWarning("[FocusOnActivation] Foco no disponible; activando directamente.", this);
        onReady?.Invoke(false);
        onFinished?.Invoke();
        return null;
    }

    private void OnDisable()
    {
        _activation?.Dispose();
        _activation = null;
    }

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
                messageDuration,
                blendInDuration
            );
        }
        else
        {
            Debug.LogWarning("[FocusOnActivation] No hay FocusManager.");
        }
    }
}
