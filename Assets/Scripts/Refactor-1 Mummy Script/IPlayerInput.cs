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
    bool ConsumeAttractDown();            // Space (atraer cajas) si lo separás
    bool IsAnyActionHeld();               // útil para bloqueos/pausa
}