using UnityEngine;

/// <summary> 
/// Datos de Tutorial: Estructura que almacena la configuración visual (posición, rotación, zoom) 
/// y el contenido del mensaje para un punto de interés tutorial específico. 
/// </summary>

public class TutorialFocusPoint : MonoBehaviour
{
    [Header("Identificador")] 
    [SerializeField] private string tutorialId;

    [Header("Cámara")] 
    [SerializeField] private Transform cameraPos;
    [SerializeField] private Transform lookAt;

    [Header("Estilo del Foco")]
    [SerializeField] private float time = 2f;
    [SerializeField] private float zoomAmount = 3.0f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Mensaje de Tutorial (Opcional)")]
    [SerializeField, TextArea(3, 10)] private string message; 
    [SerializeField] private Color textColor = Color.white;   
    [SerializeField] private float messageDuration = 2.5f;    

    public string Id => tutorialId;
    public Transform CameraPos => cameraPos;
    public Transform LookAt => lookAt;
    public float Time => time;
    public float ZoomAmount => zoomAmount;
    public AnimationCurve ZoomCurve => zoomCurve;
    public string Message => message;
    public Color TextColor => textColor;
    public float MessageDuration => messageDuration;
}