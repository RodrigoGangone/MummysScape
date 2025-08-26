/// <summary>
/// IPlayerInput
/// Contrato de inputs del jugador. Los States leen desde acá (desacoplado).
/// </summary>

public interface IPlayerInput
{
    UnityEngine.Vector2 Move { get; }     // WASD / Stick (X,Z)
    bool ConsumeShootDown();              // E
    bool ConsumeDropDown();               // Q
    bool ConsumeSmashDown();              // Space
    bool ConsumeAttractDown();            // opcional
    bool IsAnyActionHeld();               // útil para bloqueos/pausa
}