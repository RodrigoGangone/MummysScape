/// <summary> 
/// Base de Comportamiento: Clase abstracta que define el ciclo de vida estándar (Enter, Update, FixedUpdate, Exit) 
/// para cada estado de la FSM, manteniendo una referencia al controlador de estados. 
/// </summary>
public abstract class State
{
    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnFixedUpdate();
    public abstract void OnExit();

    public StateMachinePlayer StateMachine;
}