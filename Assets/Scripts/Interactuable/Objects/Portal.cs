using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utils;

public class Portal : MonoBehaviour
{
    [SerializeField] private GameObject _portalFxOff;
    [SerializeField] private GameObject _portalFxOn;
    
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnWin.Register(PassedLevelFX);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnWin.Unregister(PassedLevelFX);

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        GameEventManager.Instance.levelEvents.OnWin.Raise(SceneManager.GetActiveScene().buildIndex);
    }

    private void PassedLevelFX()
    {
        _portalFxOff.SetActive(false);
        _portalFxOn.SetActive(true);
    }
}