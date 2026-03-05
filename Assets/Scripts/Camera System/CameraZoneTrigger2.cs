using Cinemachine;
using UnityEngine;

public class CameraZoneTrigger2 : MonoBehaviour
{
    public CinemachineVirtualCamera vCam;
    public LayerMask playerLayer; // Asigna la capa de tu Player

    private BoxCollider _zone;

    void Awake()
    {
        _zone = GetComponent<BoxCollider>();
    }

    void FixedUpdate()
    {
        // Creamos una caja virtual del mismo tamaño y rotación que el trigger
        bool isPlayerInside = Physics.CheckBox(
            transform.position + _zone.center, 
            _zone.size / 2, 
            transform.rotation, 
            playerLayer
        );

        // Si el player está, prioridad 20. Si no, 10.
        int targetPriority = isPlayerInside ? 20 : 10;

        if (vCam.Priority != targetPriority)
        {
            vCam.Priority = targetPriority;
        }
    }
}
