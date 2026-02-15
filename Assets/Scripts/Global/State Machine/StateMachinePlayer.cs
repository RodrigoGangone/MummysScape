using System.Collections.Generic;
using System;
using UnityEngine;
using static Utils;

public class StateMachinePlayer : MonoBehaviour
{
    Dictionary<Enum, State> _allStates = new();
    State _currentState;
    
    private Enum _currentId;
    private IStateTransitionGuard _guard;
    public void SetGuard(IStateTransitionGuard guard) => _guard = guard;

    public void Update() { _currentState?.OnUpdate(); }
    public void FixedUpdate() { _currentState?.OnFixedUpdate(); }

    public void AddState(Enum name, State state)
    {
        if (!_allStates.ContainsKey(name))
        {
            _allStates.Add(name, state);
            state.StateMachine = this;
        }
        else
        {
            _allStates[name] = state;
        }
    }

    public Enum CurrentId()=> _currentId;
    /// <summary>
    /// Devuelve true si el estado actual es 'name'.
    /// Evita castear Enum en los llamadores y mejora legibilidad.
    /// </summary> 
    public bool IsCurrent(Enum name) => Equals(_currentId, name);

    public String getCurrentState() =>  (_currentState != null) ? _currentState.ToString() : NO_STATE;
    
    public bool ChangeState(Enum name)
    {
        if (!_allStates.ContainsKey(name) || _allStates[name].Equals(_currentState)) return false;
        //consulta al guard. Si no hay guard, deja pasar.
        if (_guard != null && !_guard.Can(_currentId, name)) return false;
        
        _currentState?.OnExit();
        _currentState = _allStates[name];
        _currentId = name;
        _currentState?.OnEnter();
        return true;
    }
    
    public bool CurrentStateImplement<T>() where T : class
    {
        return _currentState is T;
    }
    
    private void OnEnable()
    {
        // Usamos el tipo <PlayerSize> porque el evento envía el nuevo tamaño
        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerEnum.PlayerSize>(OnSizeChangedHandler);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerEnum.PlayerSize>(OnSizeChangedHandler);
    }

// El método DEBE recibir el PlayerSize aunque no lo uses directamente aquí, 
// ya que el Guard consultará el context.Model.Size actualizado.
    private void OnSizeChangedHandler(PlayerEnum.PlayerSize newSize)
    {
        if (_currentState == null || _guard == null) return;

        // Validamos: ¿Es legal estar en '_currentId' con el 'newSize' que acaba de llegar?
        // Pasamos 'null' como 'from' para que el Guard use la lógica de SizeRules.Can
        if (!_guard.Can(null, _currentId))
        {
            Debug.Log($"[StateMachine] {_currentId} no es válido para tamaño {newSize}. Reseteando a Idle.");
            ChangeState(PlayerEnum.PlayerStateId.Idle);
        }
    }
}