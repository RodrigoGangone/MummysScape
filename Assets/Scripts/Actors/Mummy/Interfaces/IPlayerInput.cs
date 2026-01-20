using UnityEngine;

public interface IPlayerInput
{
    Vector2 Move { get; }
    Vector2 AimMove { get; }

    // OnPress (Eventos de un solo frame)
    bool ConsumeAimHeld(); // AHORA SERÁ: ¿Se presionó recién?
    bool ConsumeShootDown();
    bool ConsumeDropDown();
    bool ConsumeSpaceDown();
    
    // Hold (Estado continuo)
    bool IsSpaceHeld();
    bool IsAimHeld(); // <--- NUEVO: Para saber si seguimos apuntando
    
    bool IsAnyActionHeld();
}