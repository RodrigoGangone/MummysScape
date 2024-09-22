using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    private LevelManager _levelManager;

    [SerializeField] private GameObject _portalFxOff;
    [SerializeField] private GameObject _portalFxOn;


    void Start()
    {
        _levelManager.OnPlayerWin += PassedLevelFX;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _levelManager.OnPlayerWin?.Invoke();
    }

    private void PassedLevelFX()
    {
        _portalFxOff.SetActive(false);
        _portalFxOn.SetActive(true);
    }
}