using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 
/// Nodo de Comunicación: ScriptableObject que actúa como un canal de señalización independiente, 
/// permitiendo el registro de suscriptores con soporte para 0, 1 o 2 parámetros tipados. 
/// </summary>

[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    // --- 0 parámetros ---
    private readonly List<Action> _noParam = new();

    // --- 1 parámetro ---
    private readonly List<Action<object>> _param = new();
    private readonly Dictionary<Delegate, Action<object>> _typedMap = new();

    // --- 2 parámetros ---
    private readonly List<Action<object, object>> _param2 = new();
    private readonly Dictionary<Delegate, Action<object, object>> _typedMap2 = new();

    // ======================
    // 0 PARÁMETROS
    // ======================
    public void Register(Action listener)
    {
        if (!_noParam.Contains(listener)) _noParam.Add(listener);
    }

    public void Unregister(Action listener) => _noParam.Remove(listener);

    public void Raise()
    {
        for (int i = 0; i < _noParam.Count; i++)
            _noParam[i]?.Invoke();
    }

    // ======================
    // 1 PARÁMETRO (T)
    // ======================
    public void Register<T>(Action<T> listener)
    {
        if (_typedMap.ContainsKey(listener)) return;

        Action<object> wrapper = (obj) =>
        {
            if (obj is T t) listener(t);
            else Debug.LogWarning(
                $"[GameEvent] Tipo incompatible en Raise(obj): {obj?.GetType().Name} → {typeof(T).Name}");
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
        for (int i = 0; i < _param.Count; i++)
            _param[i]?.Invoke(value);
    }

    // ======================
    // 2 PARÁMETROS (T1, T2)
    // ======================
    public void Register<T1, T2>(Action<T1, T2> listener)
    {
        if (_typedMap2.ContainsKey(listener)) return;

        Action<object, object> wrapper = (obj1, obj2) =>
        {
            if (obj1 is T1 t1 && obj2 is T2 t2)
            {
                listener(t1, t2);
            }
            else
            {
                Debug.LogWarning(
                    $"[GameEvent] Tipo incompatible en Raise(obj1, obj2): " +
                    $"({obj1?.GetType().Name}, {obj2?.GetType().Name}) → " +
                    $"({typeof(T1).Name}, {typeof(T2).Name})");
            }
        };

        _typedMap2[listener] = wrapper;
        _param2.Add(wrapper);
    }

    public void Unregister<T1, T2>(Action<T1, T2> listener)
    {
        if (_typedMap2.TryGetValue(listener, out var wrapper))
        {
            _param2.Remove(wrapper);
            _typedMap2.Remove(listener);
        }
    }

    public void Raise(object value1, object value2)
    {
        for (int i = 0; i < _param2.Count; i++)
            _param2[i]?.Invoke(value1, value2);
    }

    // ======================
    // Debug / inspección
    // ======================
    public IReadOnlyList<Action> NoParamListeners => _noParam;
    public IReadOnlyList<Action<object>> ParamListeners => _param;
    public IReadOnlyList<Action<object, object>> Param2Listeners => _param2;
}
