using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static Save;
using static Utils;

public class Portal : MonoBehaviour
{
    [SerializeField] private bool isOpen;
    private Animator Anim => GetComponentInChildren<Animator>();
    private FocusOnActivation Focus => GetComponent<FocusOnActivation>();
    [SerializeField] private PlayableDirector director; // Asigna aquí el 'Cinematic_Sarcophagus'
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnWin.Register<int>(CompleteLevel);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(CompleteLevel);

    private void Start()
    {
        if (!isOpen) return;

        Focus.Activate();
        InitLevelFx();
        
        // 1. Bloqueamos al player (si no estaba bloqueado ya)
      //  GameEventManager.Instance.playerEvents.OnLocked.Raise(true); 
        
        // 2. Iniciamos la magia
        director.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG) || isOpen) return;

        GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.A);
    }


    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG) || isOpen) return;

        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }

    private void OnTriggerStay(Collider other)
    {
        if (isOpen) return;

        if (Input.GetButtonDown("Space"))
        {
            var col = GetComponent<Collider>();
            col.enabled = false;

            PassedLevelFX();
            Focus.Activate();
        }
    }

    /// <summary>
    /// Eventos que se encargan de avisar cuando debe Iniciar le nivel y darlo por finalizado
    /// </summary>
    ///
    ///  
    public void PassedLevel() =>
        GameEventManager.Instance.levelEvents.OnWin.Raise(SceneManager.GetActiveScene().buildIndex);

    /// <summary>
    /// Fx's correspondientes a las animaciones del sarcofago
    ///
    /// Open -> Al iniciar el nivel
    /// Close -> Al finalizar el nivel
    /// </summary>
    private void PassedLevelFX() => Anim.SetTrigger("Close");
    private void InitLevelFx() => Anim.SetTrigger("Open");
}