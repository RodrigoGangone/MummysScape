using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Crear un SO por cada metodo que se vaya a utilizar
/// </summary>
[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    private List<Action> _events = new();

    public void Raise() => _events.ForEach(e => e.Invoke());

    public void RegisterEvent(Action e) => _events.Add(e);
    public void UnregisterEvent(Action e) => _events.Remove(e);
    
    public IReadOnlyList<Action> Listeners => _events; // opcional para debug
}