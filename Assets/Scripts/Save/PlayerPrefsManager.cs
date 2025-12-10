using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerPrefsManager
{
    private static readonly List<PlayerPrefsRegistry> _registries = new();

    // Vincular uno o varios registries
    public static void BindRegistry(PlayerPrefsRegistry registry)
    {
        if (registry != null && !_registries.Contains(registry))
            _registries.Add(registry);
    }
    public static void BindRegistries(params PlayerPrefsRegistry[] registries)
    {
        foreach (var r in registries) BindRegistry(r);
    }

    private static void NotifySet(string key, object value)
    {
        foreach (var r in _registries)
            if (r != null && r.Matches(key)) r.UpdateEntry(key, value);
    }
    private static void NotifyRemove(string key)
    {
        foreach (var r in _registries)
            if (r != null && r.Matches(key)) r.RemoveEntry(key);
    }
    private static void NotifyClear()
    {
        foreach (var r in _registries) r?.ClearAll();
    }

    // --------- SET / GET básicos ----------
    public static void Set<T>(string key, T value)
    {
        if (typeof(T) == typeof(int))
        {
            PlayerPrefs.SetInt(key, (int)(object)value);
        }
        else if (typeof(T) == typeof(float))
        {
            PlayerPrefs.SetFloat(key, (float)(object)value);
        }
        else if (typeof(T) == typeof(string))
        {
            PlayerPrefs.SetString(key, value?.ToString() ?? "");
        }
        else if (typeof(T) == typeof(bool))
        {
            // Bool como int 0/1
            PlayerPrefs.SetInt(key, (bool)(object)value ? 1 : 0);
        }
        else
        {
            // Tipos complejos -> JSON
            string json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString(key, json);
        }

        PlayerPrefs.Save();
        NotifySet(key, value);
    }

    public static T Get<T>(string key, T defaultValue = default)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;

        if (typeof(T) == typeof(int))
            return (T)(object)PlayerPrefs.GetInt(key);

        if (typeof(T) == typeof(float))
            return (T)(object)PlayerPrefs.GetFloat(key);

        if (typeof(T) == typeof(string))
            return (T)(object)PlayerPrefs.GetString(key);

        if (typeof(T) == typeof(bool))
        {
            // Usa el helper ya existente para bool
            bool defBool = (bool)(object)defaultValue;
            return (T)(object)GetBool(key, defBool);
        }

        // Tipos complejos: JSON
        string json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return defaultValue;

        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerPrefsManager] Error deserializando key '{key}' como {typeof(T).Name}: {e.Message}. Se usa default.");
            return defaultValue;
        }
    }
    
    public static void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        NotifyRemove(key);
    }

    public static void ClearAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        NotifyClear();
    }

    // --------- Helpers convenientes ----------
    public static void SetBool(string key, bool value) => Set(key, value ? 1 : 0);
    public static bool GetBool(string key, bool def = false) => Get<int>(key, def ? 1 : 0) == 1;
    public static int GetInt(string key, int def = 0) => Get<int>(key, def);
}
