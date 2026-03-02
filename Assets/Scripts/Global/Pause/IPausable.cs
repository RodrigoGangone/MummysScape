/// <summary> 
/// Contrato de Pausa: Interfaz universal para componentes que deben reaccionar ante cambios en el 
/// estado de pausa del juego, permitiendo habilitar o deshabilitar lógicas de forma sincronizada. 
/// </summary>

public interface IPausable
{
    void OnPauseChanged(bool paused)
    {
    }
}