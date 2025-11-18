using UnityEngine;

public interface IPlayerInput
{
    Vector2 Move { get; }
    Vector2 AimMove { get; }

    // OnPress
    bool ConsumeAimHeld();
    bool ConsumeShootDown();
    bool ConsumeDropDown();
    bool ConsumeSpaceDown();
    
    // Hold
    bool IsSpaceHeld();
    
    // Auxiliar
    bool IsAnyActionHeld();
}