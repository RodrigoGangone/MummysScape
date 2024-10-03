using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    private Player _player;
    private InteractableOutline _lastInteractableOutline; // Guarda el último objeto interactable activado.

    private void Start()
    {
        _player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        // Variable temporal para almacenar el interactable actual
        InteractableOutline currentInteractable = null;

        // Comprueba si hay interacción con Pull
        if (_player._viewPlayer.GetHitFromPull().HasValue)
        {
            currentInteractable = _player._viewPlayer.GetHitFromPull()?.transform.gameObject
                .GetComponent<InteractableOutline>();
        }

        // Solo comprueba Push si Pull no detectó nada
        if (currentInteractable == null && _player._viewPlayer.GetHitFromPush().HasValue)
        {
            currentInteractable = _player._viewPlayer.GetHitFromPush()?.transform.gameObject
                .GetComponent<InteractableOutline>();
        }

        // Solo comprueba ButtonHit si los anteriores no detectaron nada
        if (currentInteractable == null && _player._modelPlayer.ButtonHit().HasValue)
        {
            currentInteractable = _player._modelPlayer.ButtonHit()?.transform.gameObject
                .GetComponent<InteractableOutline>();
        }

        // Si encontramos un nuevo interactable que es diferente al último
        if (currentInteractable != null && currentInteractable != _lastInteractableOutline)
        {
            // Apaga el material del último objeto activado si es diferente al actual
            if (_lastInteractableOutline != null)
            {
                _lastInteractableOutline.OffMaterial();
            }

            // Enciende el material del objeto actual
            currentInteractable.OnMaterial();
            _lastInteractableOutline = currentInteractable; // Actualiza el último interactable
        }

        // Si no hay ningún objeto interactable actual y el anterior aún está encendido, apágalo
        else if (currentInteractable == null && _lastInteractableOutline != null)
        {
            _lastInteractableOutline.OffMaterial();
            _lastInteractableOutline = null;
        }
    }
}