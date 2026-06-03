using UnityEngine;
using System.Collections;

public class ActionPressureButton : BasePressureButton
{
    [Header("Action Settings")]
    public bool isOneShot;
    public bool useTimer; // Activa el comportamiento por tiempo

    public UnityEngine.Events.UnityEvent OnActivated;
    public UnityEngine.Events.UnityEvent OnDeactivated;

    private bool hasBeenActivated;
    private Coroutine releaseTimerCoroutine;

    protected override void OnPress()
    {
        if (isOneShot && hasBeenActivated) return;

        // Si el timer estaba corriendo por soltar el botón, lo cancelamos al volver a pisar
        if (releaseTimerCoroutine != null)
        {
            StopCoroutine(releaseTimerCoroutine);
            releaseTimerCoroutine = null;
        }

        // Solo lanzamos el OnActivated si no estaba ya activado
        if (!hasBeenActivated)
        {
            hasBeenActivated = true;
            OnActivated.Invoke();
        }

        if (isOneShot) this.enabled = false;
    }

    protected override void OnRelease()
    {
        if (isOneShot) return;

        if (useTimer)
        {
            // Iniciamos la cuenta regresiva usando 'timer' de tu BasePressureButton
            if (releaseTimerCoroutine != null) StopCoroutine(releaseTimerCoroutine);
            releaseTimerCoroutine = StartCoroutine(TimerRoutine());
        }
        else
        {
            // Comportamiento normal "Pressed" (inmediato)
            Deactivate();
        }
    }

    private IEnumerator TimerRoutine()
    {
        yield return new WaitForSeconds(timer);
        Deactivate();
    }

    private void Deactivate()
    {
        hasBeenActivated = false;
        OnDeactivated.Invoke();
    }
}