using UnityEngine;

public class TutorialFocusPoint : MonoBehaviour
{
    [Header("Identificador del tutorial")]
    [SerializeField] private string tutorialId;

    [Header("Cámara")]
    [SerializeField] private Transform cameraPos;
    [SerializeField] private Transform lookAt;

    [Header("Tiempos")]
    [SerializeField] private float mandatoryTime = 3f;
    [SerializeField] private float optionalTime = 2f;

    public string Id => tutorialId;
    public Transform CameraPos => cameraPos;
    public Transform LookAt => lookAt;
    public float MandatoryTime => mandatoryTime;
    public float OptionalTime => optionalTime;
}