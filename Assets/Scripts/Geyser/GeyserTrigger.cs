using UnityEngine;

public class GeyserTrigger : MonoBehaviour
{
    private Geyser _geyser;

    void Start()
    {
        _geyser = GetComponentInParent<Geyser>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
        {
            _geyser.OnPlayerEnterTrigger(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
        {
            _geyser.OnPlayerExitTrigger(other);
        }
    }
}
