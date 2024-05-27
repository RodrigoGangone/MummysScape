using System.Collections.Generic;
using UnityEngine;

public class ActivateObjectsBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platformsAll;
    [SerializeField] private TypeSandButton _typeSandButton;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Bullet")) return;

        foreach (GameObject platform in _platformsAll)
        {
            MovePlatform movePlatform = platform.GetComponent<MovePlatform>();

            if (movePlatform != null)
                movePlatform.StartAction();

            Quicksand quicksand = platform.GetComponent<Quicksand>();

            if (quicksand != null)
                quicksand.NextPosSand(_typeSandButton);
        }
    }
}

public enum TypeSandButton
{
    DownSand,
    UpSand
}