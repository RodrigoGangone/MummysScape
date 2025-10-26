using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using static Save;
using static Utils;

public class Portal : MonoBehaviour
{
    [SerializeField] private GameObject portalFxOff;
    [SerializeField] private GameObject portalFxOn;
    
    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Register(PassedLevelFX);
        GameEventManager.Instance.levelEvents.OnWin.Register<int>(CompleteLevel);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Unregister(PassedLevelFX);
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(CompleteLevel);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        GameEventManager.Instance.levelEvents.OnWin.Raise(SceneManager.GetActiveScene().buildIndex);
    }

    private void PassedLevelFX()
    {
        portalFxOff.SetActive(false);
        portalFxOn.SetActive(true);
    }
}