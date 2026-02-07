using UnityEngine;

public class TutorialFocusPoint : MonoBehaviour
{
    [Header("Identificador del tutorial")] [SerializeField]
    private string tutorialId;

    [Header("Cámara")] [SerializeField] private Transform cameraPos;
    [SerializeField] private Transform lookAt;

    [Header("Tiempos")] [SerializeField] private float time;
    public string Id => tutorialId;
    public Transform CameraPos => cameraPos;
    public Transform LookAt => lookAt;
    public float Time => time; 
}