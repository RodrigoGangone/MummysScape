using System.Collections.Generic;
using UnityEngine;

public class SwitchGroupManager : MonoBehaviour
{
    [SerializeField] private List<SwitchGroup> groupButtons;
    private SwitchGroup currentActiveButton; 

    public void NotifyPress(SwitchGroup pressedButton)
    {
        // 1. Si pisas el que ya está activo, no hacemos nada
        if (currentActiveButton == pressedButton) return;

        // 2. Si había otro activo, lo "soltamos" lógicamente
        if (currentActiveButton != null)
        {
            currentActiveButton.Deactivate();
        }

        // 3. Activamos el nuevo y lo guardamos como el actual
        currentActiveButton = pressedButton;
        currentActiveButton.Activate();
    }
}