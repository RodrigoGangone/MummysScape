using UnityEngine;

public class HippoLink : MonoBehaviour
{
    private bool _isBusy = false;
    
    public bool IsBusy => _isBusy;

    // Método para "preguntar" si se puede iniciar el viaje
    public bool CanStartTravel()
    {
        return !_isBusy;
    }

    // Método para "notificar" que el viaje comenzó
    public void StartTravel()
    {
        GameEventManager.Instance.playerEvents.OnLocked.Raise(true);
        _isBusy = true;
    }

    // Método para "notificar" que el viaje terminó
    public void EndTravel()
    {
        GameEventManager.Instance.playerEvents.OnLocked.Raise(false);
        _isBusy = false;
    }
}
