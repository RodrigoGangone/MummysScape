using UnityEngine;

public class CameraZoneTrigger : MonoBehaviour
{
    [SerializeField] private DollyPositionManager dollyManager;
    [SerializeField] private int waypointIndex; // 0..8 según la zona

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        dollyManager.SetZoneDollyPosition(waypointIndex);
    }
}
