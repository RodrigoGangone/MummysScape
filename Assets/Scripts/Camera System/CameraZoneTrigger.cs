using UnityEngine;

public class CameraZoneTrigger : MonoBehaviour
{
    [SerializeField] private DollyPositionManager dollyManager;
    [SerializeField] private string dollyTrackId; // Ej: "Ground", "Water"
    [SerializeField] private int waypointIndex;   // 0..N según la zona

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        if (dollyManager == null) return;

        if (string.IsNullOrEmpty(dollyTrackId))
        {
            // Compatibilidad: usa el track actual del DollyManager
            dollyManager.SetZoneDollyPosition(waypointIndex);
        }
        else
        {
            // Nuevo sistema multi-dolly
            dollyManager.SetZoneDollyPosition(dollyTrackId, waypointIndex);
        }
    }
}