using System.Linq;
using UnityEngine;
using System.Collections;

public class GroupPressureManager : MonoBehaviour
{
    public GroupPressureButton[] buttons;
    
    [Header("Timer Settings")]
    public bool useTimer;
    public float delayTimer;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnAllActive;
    public UnityEngine.Events.UnityEvent OnGroupDeactivated;

    private bool isGroupActive;
    private Coroutine timerCoroutine;

    public void NotifyButtonPressed()
    {
        if (isGroupActive) return;

        // Si todos los botones están lógicamente trabados, activamos el mecanismo
        if (buttons.All(b => b.IsActive))
        {
            isGroupActive = true;
            OnAllActive.Invoke();
        }
    }

    public void EvaluateTimerCondition()
    {
        // El timer solo nos importa si el puzzle ya fue resuelto
        if (!isGroupActive) return;

        // Verificamos si TODAVÍA hay una caja o la Momia pisando físicamente AL MENOS UN botón
        bool isAnyoneStandingOnAButton = buttons.Any(b => b.IsPhysicallyOccupied);

        if (!isAnyoneStandingOnAButton && useTimer)
        {
            // Ya nadie pisa nada: arranca la cuenta regresiva
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(ResetGroupRoutine());
        }
        else if (isAnyoneStandingOnAButton)
        {
            // Alguien volvió a pisar un botón antes de que termine el tiempo: cancelamos el reseteo
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }
    }

    private IEnumerator ResetGroupRoutine()
    {
        yield return new WaitForSeconds(delayTimer);
        ResetEntireGroup();
    }

    private void ResetEntireGroup()
    {
        isGroupActive = false;
        OnGroupDeactivated.Invoke(); // Cerramos la puerta / apagamos el mecanismo

        // Destrabamos todos los botones para que el jugador tenga que volver a pisarlos
        foreach (var btn in buttons)
        {
            btn.ResetButton();
        }
    }
}