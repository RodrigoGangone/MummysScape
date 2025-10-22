using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameEvent – versión corregida de registro tipado
/// </summary>
[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    private readonly List<Action> _noParam = new();
    private readonly List<Action<object>> _param = new();

    // Mapa para poder desuscribir correctamente Action<T>
    private readonly Dictionary<Delegate, Action<object>> _typedMap = new();

    public void Register(Action listener)
    {
        if (!_noParam.Contains(listener)) _noParam.Add(listener);
    }
    public void Unregister(Action listener) => _noParam.Remove(listener);
    public void Raise() { for (int i = 0; i < _noParam.Count; i++) _noParam[i]?.Invoke(); }

    public void Register<T>(Action<T> listener)
    {
        if (_typedMap.ContainsKey(listener)) return;
        Action<object> wrapper = (obj) =>
        {
            if (obj is T t) listener(t);
            else Debug.LogWarning($"[GameEvent] Tipo incompatible en Raise(): {obj?.GetType().Name} → {typeof(T).Name}");
        };
        _typedMap[listener] = wrapper;
        _param.Add(wrapper);
    }

    public void Unregister<T>(Action<T> listener)
    {
        if (_typedMap.TryGetValue(listener, out var wrapper))
        {
            _param.Remove(wrapper);
            _typedMap.Remove(listener);
        }
    }

    public void Raise(object value)
    {
        for (int i = 0; i < _param.Count; i++) _param[i]?.Invoke(value);
    }

    public IReadOnlyList<Action> NoParamListeners => _noParam;
    public IReadOnlyList<Action<object>> ParamListeners => _param;
}