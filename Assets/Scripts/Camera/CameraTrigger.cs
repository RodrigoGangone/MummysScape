using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    [SerializeField] private CameraNode targetNode;  // Nodo al que debe moverse la cámara
    private CameraPathManager cameraPathManager;

    private void Start()
    {
        cameraPathManager = Camera.main.GetComponent<CameraPathManager>();
        if (cameraPathManager == null)
        {
            Debug.LogError("CameraPathManager no encontrado en la cámara principal.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather") && cameraPathManager != null && targetNode != null)
        {
            cameraPathManager.MoveCameraToNode(targetNode);
        }
    }
}