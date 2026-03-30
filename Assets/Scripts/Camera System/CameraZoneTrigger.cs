using UnityEngine;
using Cinemachine;
using static Tags;

/// <summary> 
/// Gatillo de Cámara: Gestiona la prioridad de las cámaras virtuales de Cinemachine al detectar la entrada o salida 
/// del jugador en zonas específicas, permitiendo cambios de perspectiva automáticos en el nivel. 
/// </summary>

public class CameraZoneTrigger : MonoBehaviour
{
    [Header("Arrastra aquí la VCam de ESTA isla")]
    public CinemachineVirtualCamera vCam;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
            vCam.Priority = 20;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
            vCam.Priority = 10;
    }
}