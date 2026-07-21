using UnityEngine;
using static Tags;

public class Spike : MonoBehaviour
{
    private bool _hasKilledPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        KillPlayer();
    }

    public void KillPlayer()
    {
        if (_hasKilledPlayer) return;

        _hasKilledPlayer = true;

        GameEventManager.Instance.levelEvents.OnDeath.Raise();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        _hasKilledPlayer = false;
    }
}