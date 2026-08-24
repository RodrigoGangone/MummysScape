/// <summary> 
/// Diccionario de Estados: Centraliza las enumeraciones fundamentales del jugador, definiendo 
/// los estados de la FSM (PlayerStateId) y los niveles de tamaño (PlayerSize) basados en el inventario. 
/// </summary>

public static class PlayerEnum
{
    public enum PlayerSize { Normal, Small, Head, Empowered}
    public enum PlayerStateId { Idle, Walk, Fall, Aim, Shoot, Smash, DropBandage, Push, Attract, Swing, QuickTravel, KnockBack, Fake, Dead, Win }
}
