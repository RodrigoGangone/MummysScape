using UnityEngine;
using UnityEngine.Events;

public class SwitchGroup : BasePressureButton
{
    public SwitchGroupManager manager;
    public string switchID; 
    
    [Header("Logic Events")]
    public UnityEvent OnActivated;   // Se dispara al volverse el activo
    public UnityEvent OnDeactivated; // Se dispara cuando otro toma su lugar

    protected override void OnPress()
    {
        // Al pisarlo, el Manager decide si se activa
        manager.NotifyPress(this);
    }

    protected override void OnRelease() 
    {
        // Mantenemos la persistencia: no hace nada al salir
    }

    public void Activate() => OnActivated.Invoke();
    public void Deactivate() => OnDeactivated.Invoke();
}