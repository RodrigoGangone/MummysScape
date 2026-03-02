/// <summary> 
/// Registro de Bloqueo: Clase estática que gestiona las banderas globales de Pausa y Bloqueo (Lock), 
/// permitiendo que los sistemas consulten si la lógica de juego debe restringirse por menús o cinemáticas. 
/// </summary>

public static class PlayerControlState
{
    public static bool Paused { get; private set; }
    public static bool Locked { get; private set; }

    public static bool AnyBlocked => Paused || Locked;

    public static void SetPause(bool v) => Paused = v;
    public static void SetLock(bool v) => Locked = v;
}