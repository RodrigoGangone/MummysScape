using UnityEngine;
using static Utils;

public class Portal : MonoBehaviour
{
    private LevelManager _levelManager;

    [SerializeField] private GameObject _portalFxOff;
    [SerializeField] private GameObject _portalFxOn;


    void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();
        _levelManager.OnPlayerWin += PassedLevelFX;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        _levelManager.OnPlayerWin?.Invoke();
    }

    private void PassedLevelFX()
    {
        _portalFxOff.SetActive(false);
        _portalFxOn.SetActive(true);
    }
}