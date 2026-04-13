using System.Collections.Generic;
using System;
using UnityEngine;
using static Utils;

/// <summary> 
/// Cerebro de la FSM: Administra el diccionario de estados disponibles y orquesta las transiciones 
/// mediante un sistema de validación (Guards), además de asegurar que el estado actual sea válido para el tamaño del jugador. 
/// </summary>

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
// Agregá esto a tu StateMachinePlayer.cs
    public State GetState(Enum name)
    {
        if (_allStates.TryGetValue(name, out var state))
        {
            return state;
        }
        return null;
    }
    public bool IsCurrent(Enum name) => Equals(_currentId, name);

    public String getCurrentState() =>  (_currentState != null) ? _currentState.ToString() : NO_STATE;
    
    public bool ChangeState(Enum name)
    {
        if (!_allStates.ContainsKey(name) || _allStates[name].Equals(_currentState)) return false;

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
    
    private void OnSizeChangedHandler(PlayerEnum.PlayerSize newSize)
    {
        if (_currentState == null || _guard == null) return;

        if (!_guard.Can(null, _currentId))
        {
            Debug.Log($"[StateMachine] {_currentId} no es válido para tamaño {newSize}. Reseteando a Idle.");
            ChangeState(PlayerEnum.PlayerStateId.Idle);
        }
    }
    
    private void OnEnable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerEnum.PlayerSize>(OnSizeChangedHandler);
    private void OnDisable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerEnum.PlayerSize>(OnSizeChangedHandler);
}