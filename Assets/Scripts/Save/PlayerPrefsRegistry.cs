using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prefs/PlayerPrefsRegistry")]
public class PlayerPrefsRegistry : ScriptableObject
{
    // -------- NUEVO: Preset por enum --------
    [System.Flags]
    public enum RegistryKeyPreset
    {
        None            = 0,
        Gems            = 1 << 0, // "gem."
        GemTotals       = 1 << 1, // "gemTotal."
        LevelCompleted  = 1 << 2, // "level.completed."
        Time            = 1 << 3, // "level.completed."
        VolumeSound     = 1 << 4, // "level.completed."
        VolumeFX        = 1 << 5, // "level.completed."
        All             = ~0
    }

    [Header("Preset de claves (opcional)")]
    [SerializeField] private RegistryKeyPreset preset = RegistryKeyPreset.None;
    [SerializeField] private bool lockToPreset = true;

    // -------- Ya existente: prefijos que acepta este registry --------
    [Header("Qué keys acepta este Registry (prefijos). Se autollenan con el preset si 'lockToPreset' está activo.")]
    [SerializeField] private string[] keyPrefixes;

    // Almacenamiento visual/inspector
    [SerializeField] private List<string> keys = new();
    [SerializeField] private List<string> values = new();

    // --- Accesores por si los necesita el Editor ---
    public RegistryKeyPreset Preset => preset;
    public bool LockToPreset => lockToPreset;
    public string[] KeyPrefixes => keyPrefixes;

    // Sincroniza prefijos si está bloqueado al preset
    private void OnValidate()
    {
        if (lockToPreset && preset != RegistryKeyPreset.None)
            keyPrefixes = PresetToPrefixes(preset);
    }

    // --------- Mapeo de preset a prefijos ----------
// ... cabeceras y campos existentes ...

// Mapea tu preset de Flags → array de prefijos, usando el catálogo PrefKeys
    public static string[] PresetToPrefixes(RegistryKeyPreset p)
    {
        var list = new List<string>();

        void Add(PrefFamily fam)
        {
            if (PrefKeys.Prefix.TryGetValue(fam, out var pref))
                list.Add(pref);
        }

        if (p.HasFlag(RegistryKeyPreset.Gems))           Add(PrefFamily.Gems);
        if (p.HasFlag(RegistryKeyPreset.GemTotals))      Add(PrefFamily.GemTotals);
        if (p.HasFlag(RegistryKeyPreset.LevelCompleted)) Add(PrefFamily.LevelCompleted);
        if (p.HasFlag(RegistryKeyPreset.Time))           Add(PrefFamily.Time);
        if (p.HasFlag(RegistryKeyPreset.VolumeSound))    Add(PrefFamily.VolumeSound);
        if (p.HasFlag(RegistryKeyPreset.VolumeFX))       Add(PrefFamily.VolumeFX);

        return list.ToArray();
    }

    
    // --------- Filtro por prefijos ----------
    public bool Matches(string key)
    {
        if (keyPrefixes == null || keyPrefixes.Length == 0) return true;
        foreach (var p in keyPrefixes)
        {
            if (!string.IsNullOrEmpty(p) && key.StartsWith(p, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    // --------- API para reflejar en inspector ----------
    public void UpdateEntry(string key, object value)
    {
        string s = value?.ToString() ?? "null";
        int idx = keys.IndexOf(key);
        if (idx >= 0) values[idx] = s;
        else { keys.Add(key); values.Add(s); }
    }

    public void RemoveEntry(string key)
    {
        int idx = keys.IndexOf(key);
        if (idx >= 0) { keys.RemoveAt(idx); values.RemoveAt(idx); }
    }

    public void ClearAll()
    {
        keys.Clear();
        values.Clear();
    }
}
