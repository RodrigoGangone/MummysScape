using UnityEngine;
using Cinemachine;

public class CameraZoneTrigger : MonoBehaviour
{
    [Header("Arrastra aquí la VCam de ESTA isla")]
    public CinemachineVirtualCamera vCam;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
        {
            // CinemachineBrain siempre elige la cámara con mayor prioridad.
            // Al entrar, subimos esta a 20 (encima del default que suele ser 10).
            vCam.Priority = 20;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
        {
            // Al salir, la bajamos. Si entra en otra zona, la otra subirá a 20.
            vCam.Priority = 10;
        }
    }
}