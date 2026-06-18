using UnityEngine;

/// <summary> 
/// Contrato de Entrada: Abstrae todos los comandos del jugador (movimiento, apuntado y acciones), 
/// diferenciando claramente entre eventos de un solo frame (OnPress) y estados continuos (Hold).
/// </summary>

public interface IPlayerInput
{
    Vector2 Move { get; }
    Vector2 AimMove { get; }

    bool ConsumeAimDown();
    bool ConsumeAimUp();

    bool ConsumeDropDown();
    bool ConsumeSpaceDown();

    bool ConsumeCancelAim();

    bool IsSpaceHeld();
    bool IsAimHeld();
}