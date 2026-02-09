using UnityEngine;

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

    public string Id => tutorialId;
    public Transform CameraPos => cameraPos;
    public Transform LookAt => lookAt;
    public float Time => time;
    public float ZoomAmount => zoomAmount;
    public AnimationCurve ZoomCurve => zoomCurve;
}