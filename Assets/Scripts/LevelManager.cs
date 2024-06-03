using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem _fxPortal;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _fxPortal.Play();

        other.GetComponent<Player>().enabled = false;

        Invoke(nameof(ChangeScene), 7f);
    }

    private void ChangeScene()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Level1":
                SceneManager.LoadScene("Level2");
                break;
            case "Level2":
                SceneManager.LoadScene("Level3");
                break;
            case "Level3":
                SceneManager.LoadScene("Level1"); //TODO: Seguir agregando niveles
                break;
        }
    }
}