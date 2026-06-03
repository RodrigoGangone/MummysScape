using UnityEngine;

public class GroupPressureButton : BasePressureButton
{
    public GroupPressureManager manager;
    
    private bool isLockedDown;

    protected override void OnPress()
    {
        // Si no estaba trabado, lo trabamos y le avisamos al manager
        if (!isLockedDown)
        {
            isLockedDown = true;
            manager.NotifyButtonPressed();
        }
        
        // Avisamos que alguien pisó para cancelar el timer si estaba corriendo
        manager.EvaluateTimerCondition();
    }

    protected override void OnRelease()
    {
        // No destrabamos el botón acá (se queda presionado).
        // Solo avisamos al manager para que revise si debe arrancar la cuenta regresiva.
        manager.EvaluateTimerCondition();
    }

    // Estado Lógico: ¿El botón está hundido/trabado?
    public bool IsActive => isLockedDown;

    // Estado Físico: ¿Hay alguien parado encima AHORA mismo? (Usa la variable de BasePressureButton)
    public bool IsPhysicallyOccupied => isOccupied;

    // El manager llamará a este método cuando se acabe el tiempo y haya que resetear el puzzle
    public void ResetButton()
    {
        isLockedDown = false; 
        // Tip: Acá podés agregar un UnityEvent OnReset visual si necesitás que el botón vuelva a subir en la animación
    }
}