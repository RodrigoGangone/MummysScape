using static Tags;
using UnityEngine;

public class BossAngryDetection : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        GameEventManager.Instance.bossEvents.OnAngry.Raise();

        Destroy(gameObject);
    }
}