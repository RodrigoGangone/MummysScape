using UnityEngine;

/// <summary> 
/// Coordinador de Viaje: Actúa como el controlador de estado para el sistema de transporte, 
/// gestionando la disponibilidad del enlace y disparando eventos globales para bloquear 
/// o liberar el control del jugador durante la transición. 
/// </summary>

public class HippoLink : MonoBehaviour
{
    private bool _isBusy = false;
    
    public bool IsBusy => _isBusy;

    public bool CanStartTravel() => !_isBusy;

    public void StartTravel()
    {
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("HippoTravel", true);
        _isBusy = true;
    }

    public void EndTravel()
    {
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("HippoTravel", false);
        _isBusy = false;
    }
}
