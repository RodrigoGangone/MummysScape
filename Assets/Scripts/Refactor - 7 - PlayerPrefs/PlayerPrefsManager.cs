using UnityEngine;

public static class PlayerPrefsManager
{
    private static PlayerPrefsRegistry _registry;
    public static void BindRegistry(PlayerPrefsRegistry registry) => _registry = registry;
    
    public static void Set<T>(string key, T value)
    {
        if (typeof(T) == typeof(int))
            PlayerPrefs.SetInt(key, (int)(object)value);
        else if (typeof(T) == typeof(float))
            PlayerPrefs.SetFloat(key, (float)(object)value);
        else if (typeof(T) == typeof(string))
            PlayerPrefs.SetString(key, value.ToString());
        else
        {
            string json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString(key, json);
        }

        PlayerPrefs.Save();

        _registry.UpdateEntry(key, value); // Actualiza registro visible
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

        string json = PlayerPrefs.GetString(key);
        return JsonUtility.FromJson<T>(json);
    }

    public static void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
        _registry?.RemoveEntry(key);
    }

    public static void ClearAll()
    {
        PlayerPrefs.DeleteAll();
        _registry?.ClearAll();
    }
}