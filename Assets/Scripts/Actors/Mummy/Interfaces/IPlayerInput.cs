using UnityEngine;

/// <summary> 
/// Contrato de Entrada: Abstrae todos los comandos del jugador (movimiento, apuntado y acciones), 
/// diferenciando claramente entre eventos de un solo frame (OnPress) y estados continuos (Hold).
/// </summary>

public interface IPlayerInput
{
    Vector2 Move { get; }
    Vector2 AimMove { get; }

    // OnPress (Eventos de un solo frame)
    bool ConsumeAimHeld();
    bool ConsumeShootDown();
    bool ConsumeDropDown();
    bool ConsumeSpaceDown();
    
    // Hold (Estado continuo)
    bool IsSpaceHeld();
    bool IsAimHeld();
    
    bool IsAnyActionHeld();
}