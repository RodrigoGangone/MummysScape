/// <summary>
/// IPushable
/// Contrato para objetos empujables. Permite bloquear eje (X o Z) mientras el player está en contacto.
/// </summary>
public interface IPushable
{
    void LockAxisX();   // Permitir movimiento solo en X
    void LockAxisZ();   // Permitir movimiento solo en Z
    void UnlockAxes();  // Permitir X/Z (sin diagonal se controla por fricción/entrada del jugador)
}