using UnityEngine;

public class MovePlatfornNoPush : MonoBehaviour
{
    private MovePlatform _movePlatform;

    private void Start()
    {
        _movePlatform = GetComponentInParent<MovePlatform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
            _movePlatform.speed = 0;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
            _movePlatform.speed = 1;
    }
}