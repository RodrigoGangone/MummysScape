using System.Collections.Generic;
using UnityEngine;

public class ActivateObjectsBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platformsAll;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Bullet")) return;
        
        Debug.Log("ENTRE A ACTIBAOJECTS");

        foreach (GameObject platform in _platformsAll)
        {
            MovePlatform movePlatform = platform.GetComponent<MovePlatform>();

            if (movePlatform != null)
                movePlatform.StartAction();

            QuickSandNEW quicksand = platform.GetComponent<QuickSandNEW>();

            if (quicksand != null)
                quicksand.ActivateSand();
        }
    }
}