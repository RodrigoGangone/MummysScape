using UnityEngine;

public class MovePlatfornNoPush : MonoBehaviour
{
    private MoveHorizontalPlatform _moveHorizontalPlatform;

    private void Start()
    {
        _moveHorizontalPlatform = GetComponentInParent<MoveHorizontalPlatform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerFather"))
        {
            Debug.Log("COLISIONO CON PLAYER");
            _moveHorizontalPlatform.ReturnToPrevious();
        }
    }
}