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
        {
            Debug.Log("COLISIONO CON PLAYER");
            _movePlatform.ReturnToPrevious();
        }
    }
}