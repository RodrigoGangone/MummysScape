using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    public void ChangeScene()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Modular1":
                SceneManager.LoadScene("Modular2");
                break;
            case "Modular2":
                SceneManager.LoadScene("Modular3");
                break;
            case "Modular3":
                SceneManager.LoadScene("Modular1"); //TODO: Seguir agregando niveles
                break;
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
            ChangeScene();
    }
}