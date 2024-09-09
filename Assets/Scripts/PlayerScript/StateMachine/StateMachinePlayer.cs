using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static Utils;

public class StateMachinePlayer : MonoBehaviour
{
    Dictionary<Enum, State> _allStates = new();
    State _currentState;

    public void Update()
    {
        _currentState?.OnUpdate();
    }

    public void FixedUpdate()
    {
        _currentState?.OnFixedUpdate();
    }

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

    public String getCurrentState()
    {
        return (_currentState != null) ? _currentState.ToString() : NO_STATE;
    }

    public void ChangeState(Enum name)
    {
        if (_allStates.ContainsKey(name) && !_allStates[name].Equals(_currentState))
        {
            _currentState?.OnExit();
            if (_allStates.ContainsKey(name)) _currentState = _allStates[name];
            _currentState?.OnEnter();
        }
    }
}