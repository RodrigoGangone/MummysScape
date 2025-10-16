using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameEvent flexible que permite disparar eventos con o sin parámetro.
/// </summary>
[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    private readonly List<Action> _noParamListeners = new();
    private readonly List<Action<object>> _paramListeners = new();
    
    public void Register(Action listener)
    {
        if (!_noParamListeners.Contains(listener))
            _noParamListeners.Add(listener);
    }
    public void Unregister(Action listener) => _noParamListeners.Remove(listener);
    public void Raise() => _noParamListeners.ForEach(e => e?.Invoke());

    public void Register<T>(Action<T> listener)
    {
        // Evita duplicados si ya se registró el mismo método
        if (!_paramListeners.Exists(a => a.Method == listener.Method && a.Target == listener.Target))
            _paramListeners.Add(Wrapper);

        // Wrapper para convertir Action<T> en Action<object>
        void Wrapper(object obj)
        {
            if (obj is T t) listener(t);
            else Debug.LogWarning($"[GameEvent] Tipo incompatible en Raise(): {obj?.GetType().Name} → {typeof(T).Name}");
        }
    }
    
    public void Unregister<T>(Action<T> listener) => _paramListeners.RemoveAll(a => a.Method == listener.Method 
                                                                                                   && a.Target == listener.Target);
    
    public void Raise(object value) => _paramListeners.ForEach(e => e?.Invoke(value));

    // --- Debug ---
    public IReadOnlyList<Action> NoParamListeners => _noParamListeners;
    public IReadOnlyList<Action<object>> ParamListeners => _paramListeners;
}