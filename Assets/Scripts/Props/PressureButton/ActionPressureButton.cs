using UnityEngine;

public class ActionPressureButton : BasePressureButton
{
    public bool isOneShot;
    public UnityEngine.Events.UnityEvent OnActivated;
    public UnityEngine.Events.UnityEvent OnDeactivated;

    private bool hasBeenActivated;

    protected override void OnPress()
    {
        if (isOneShot && hasBeenActivated) return;

        hasBeenActivated = true;
        OnActivated.Invoke();
        if (isOneShot) this.enabled = false; // Se apaga el script
    }

    protected override void OnRelease()
    {
        if (!isOneShot) OnDeactivated.Invoke();
    }
}
