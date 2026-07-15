using System.Collections.Generic;
using UnityEngine;

public class SwitchGroupManager : MonoBehaviour
{
    [SerializeField] private List<SwitchGroup> groupButtons;
    private SwitchGroup currentActiveButton; 

    public void NotifyPress(SwitchGroup pressedButton)
    {
        Debug.Log($"[Manager: {this.gameObject.name}] Recibe señal de: {pressedButton.gameObject.name}. Activo actual: {(currentActiveButton != null ? currentActiveButton.gameObject.name : "Ninguno")}");

        // 1. Si pisas el que ya está activo, no hacemos nada
        if (currentActiveButton == pressedButton)
        {
            Debug.Log($"[Manager] {pressedButton.gameObject.name} ya es el activo. Ignorando.");
            return;
        }

        // 2. Si había otro activo, lo "soltamos" lógicamente
        if (currentActiveButton != null)
        {
            Debug.Log($"[Manager] Apagando el anterior: {currentActiveButton.gameObject.name}");
            currentActiveButton.Deactivate();
        }

        // 3. Activamos el nuevo y lo guardamos como el actual
        Debug.Log($"[Manager] Activando el nuevo: {pressedButton.gameObject.name}");
        currentActiveButton = pressedButton;
        currentActiveButton.Activate();
    }
}