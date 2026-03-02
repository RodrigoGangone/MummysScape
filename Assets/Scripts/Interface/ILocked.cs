/// <summary> 
/// Receptor de Bloqueo: Define el método de respuesta para componentes que deben habilitar 
/// o deshabilitar su lógica cuando el control del jugador es restringido globalmente. 
/// </summary>

public interface ILocked
{
    void OnLockChanged(bool isLocked);
}