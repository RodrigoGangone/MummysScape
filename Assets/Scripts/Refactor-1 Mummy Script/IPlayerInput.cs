using UnityEngine;

/// <summary>
/// IPlayerInput
/// Contrato de inputs del jugador. Los States leen desde acá (desacoplado).
/// </summary>

public interface IPlayerInput
{
    Vector2 Move { get; }                   // WASD / Stick (X,Z)
    
    // OnPress
    bool ConsumeShootDown();                // E
    bool ConsumeDropDown();                 // Q
    bool ConsumeSpaceDown();                // Space (inicio del hold) - por si queremos detectar arranque
    
    // Hold
    bool IsSpaceHeld();              // Space (mantener) -> Attract & Swing
    
    // Auxiliar
    bool IsAnyActionHeld();                 // útil para bloqueos/pausa
}