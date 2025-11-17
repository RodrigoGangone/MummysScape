using System.Collections;
using Cinemachine;
using UnityEngine;

public class FocusOnActivation : MonoBehaviour
{
    private enum FocusMode
    {
        OneShotTimed, 
        TutorialInput 
    }

    [Header("Configuración General")]
    [SerializeField] private FocusMode mode = FocusMode.OneShotTimed;
    [SerializeField] private CinemachineVirtualCamera gameplayCam;
    [SerializeField] private CinemachineVirtualCamera activateCam;

    [Header("Objetos de Foco")] 
    [SerializeField] private Transform cameraFocusPos;
    [SerializeField] private Transform cameraFocusLookAt;

    [Header("Modo 'OneShotTimed' (Objetos)")] 
    [SerializeField] private float focusDuration = 2f;

    [Header("Modo 'TutorialInput'")]
    [SerializeField] private float mandatoryViewTime = 3f;

    private bool _isPlaying;
    private bool _firstActivationDone;
    private bool _playerInsideTrigger;

    public void Activate()
    {
        if (_isPlaying) return;

        switch (mode)
        {
            case FocusMode.OneShotTimed:
                StartCoroutine(FocusRoutine(
                    enforceMandatoryTime: false,
                    exitByInputOnly: false
                ));
                break;

            case FocusMode.TutorialInput:
                bool enforceMandatory = !_firstActivationDone;
                StartCoroutine(FocusRoutine(
                    enforceMandatoryTime: enforceMandatory,
                    exitByInputOnly: true
                ));
                break;
        }
    }

    private IEnumerator FocusRoutine(bool enforceMandatoryTime, bool exitByInputOnly)
    {
        _isPlaying = true;

        // Seteo de la cámara de foco
        activateCam.Follow = cameraFocusLookAt;
        activateCam.LookAt = cameraFocusLookAt;
        activateCam.transform.position = cameraFocusPos.position;

        gameplayCam.Priority = 0;
        activateCam.Priority = 10;

        if (exitByInputOnly && enforceMandatoryTime && !_firstActivationDone)
        {
            yield return new WaitForSeconds(mandatoryViewTime);

            gameplayCam.Priority = 10;
            activateCam.Priority = 0;

            activateCam.Follow = null;
            activateCam.LookAt = null;

            _firstActivationDone = true;
            _isPlaying = false;
            yield break;
        }

        if (!exitByInputOnly)
        {
            yield return new WaitForSeconds(focusDuration);
        }

        // -----------------------------
        //   VOLVER A GAMEPLAY
        // -----------------------------
        gameplayCam.Priority = 10;
        activateCam.Priority = 0;

        activateCam.Follow = null;
        activateCam.LookAt = null;

        _isPlaying = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _playerInsideTrigger = true;

        if (mode == FocusMode.TutorialInput)
        {
            if (!_firstActivationDone && !_isPlaying)
            {
                StartCoroutine(FocusRoutine(
                    enforceMandatoryTime: true,
                    exitByInputOnly: true
                ));
            }
        }
    }
}
