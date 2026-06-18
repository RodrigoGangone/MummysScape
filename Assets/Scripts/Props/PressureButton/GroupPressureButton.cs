using UnityEngine;

public class GroupPressureButton : BasePressureButton
{
    public GroupPressureManager manager;
    
    private bool isLockedDown;

    protected override void OnPress()
    {
        // 1. Llamamos a la base para que se ejecute la animación de hundirse
        base.OnPress(); 

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
        // 2. Opcional: Puedes llamar a base.OnRelease() si quieres que el 
        // botón visualmente suba aunque siga "lógicamente" trabado. 
        // Si el botón debe quedarse hundido visualmente hasta el Reset, borra esta línea.
        base.OnRelease(); 

        manager.EvaluateTimerCondition();
    }

    // 3. NUEVO: Implementamos el intento fallido
    protected override void OnFailedPress()
    {
        base.OnFailedPress(); // Ejecuta el Debug.Log o Sonido/Animación base de error

        // Opcional: Si necesitas que el manager se entere de los errores 
        // (por ejemplo, para resetear el puzzle inmediatamente si se equivocan).
        // manager.NotifyFailedPress(); 
    }

    // Estado Lógico: ¿El botón está hundido/trabado?
    public bool IsActive => isLockedDown;

    // 4. ACTUALIZADO: Reemplazamos 'isOccupied' por el nuevo estado.
    // Usamos 'ValidPress' para que el manager solo cuente el peso correcto.
    // Si querés que el peso incorrecto también detenga el timer, cambialo a: 
    // => currentState != ButtonState.Empty;
    public bool IsPhysicallyOccupied => currentState == ButtonState.ValidPress;

    // El manager llamará a este método cuando se acabe el tiempo y haya que resetear el puzzle
    public void ResetButton()
    {
        isLockedDown = false; 
        
        // Extra: Si el botón se quedó visualmente hundido, acá deberías forzar la animación a subir
        // base.OnRelease();
    }
}