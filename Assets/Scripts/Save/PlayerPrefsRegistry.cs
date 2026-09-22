using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 
/// Registro de Depuración: ScriptableObject que permite visualizar y filtrar en el Inspector 
/// qué datos están guardados actualmente en el disco duro basándose en prefijos. 
/// </summary>

[CreateAssetMenu(menuName = "Prefs/PlayerPrefsRegistry")]
public class PlayerPrefsRegistry : ScriptableObject
{
    // -------- NUEVO: Preset por enum --------
    [System.Flags]
    public enum RegistryKeyPreset
    {
        None            = 0,
        Gems            = 1 << 0,
        GemTotals       = 1 << 1,
        LevelCompleted  = 1 << 2,
        Time            = 1 << 3,
        VolumeSound     = 1 << 4,
        VolumeFX        = 1 << 5,
        Seen            = 1 << 6, 
        Navigation      = 1 << 7, 
        All             = ~0
    }

    [Header("Preset de claves (opcional)")]
    [SerializeField] private RegistryKeyPreset preset = RegistryKeyPreset.None;
    [SerializeField] private bool lockToPreset = true;

    // -------- Ya existente: prefijos que acepta este registry --------
    [Header("Qué keys acepta este Registry (prefijos). Se autollenan con el preset si 'lockToPreset' está activo.")]
    [SerializeField] private string[] keyPrefixes;

    // Datos heredados: se conservan como lista inicial de claves conocidas.
    // La partida y el inspector nunca deben modificar estos campos serializados.
    [SerializeField] private List<string> keys = new();
    [SerializeField] private List<string> values = new();

    [NonSerialized] private List<KeyValuePair<string, string>> entries;

    public IReadOnlyList<KeyValuePair<string, string>> Entries => RuntimeEntries;

    private List<KeyValuePair<string, string>> RuntimeEntries
    {
        get
        {
            if (entries == null)
            {
                entries = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < keys.Count; i++)
                    entries.Add(new KeyValuePair<string, string>(keys[i], i < values.Count ? values[i] : ""));
            }
            return entries;
        }
    }

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
        if (p.HasFlag(RegistryKeyPreset.Seen))           Add(PrefFamily.Seen);
        if (p.HasFlag(RegistryKeyPreset.Navigation))     Add(PrefFamily.Navigation);
        

        return list.ToArray();
    }

    
    public bool Matches(string key)
    {
        if (keyPrefixes == null || keyPrefixes.Length == 0) return true;
        foreach (var p in keyPrefixes)
        {
            if (!string.IsNullOrEmpty(p) &&
                key.StartsWith(p, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

// --------- API para reflejar en inspector ----------
    public void UpdateEntry(string key, object value)
    {
        var data = RuntimeEntries;
        var entry = new KeyValuePair<string, string>(key, value?.ToString() ?? "null");
        int idx = data.FindIndex(item => item.Key == key);
        if (idx >= 0) data[idx] = entry;
        else data.Add(entry);
    }

    public void RemoveEntry(string key)
    {
        RuntimeEntries.RemoveAll(item => item.Key == key);
    }

    public void ClearAll()
    {
        RuntimeEntries.Clear();
    }

    public void SortEntries(Comparison<KeyValuePair<string, string>> comparison)
    {
        RuntimeEntries.Sort(comparison);
    }

    public void MoveEntry(int index, int direction)
    {
        var data = RuntimeEntries;
        int newIndex = index + direction;
        if (index < 0 || index >= data.Count || newIndex < 0 || newIndex >= data.Count) return;
        var entry = data[index];
        data.RemoveAt(index);
        data.Insert(newIndex, entry);
    }

}
