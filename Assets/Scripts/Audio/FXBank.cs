using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 
/// Base de Datos de Sonidos: ScriptableObject que organiza colecciones de efectos (FxEntry) por categorías, 
/// permitiendo su ejecución mediante claves (keys) y configurando parámetros de espacialización. 
/// </summary>
[CreateAssetMenu(fileName = "FxBank", menuName = "Audio/Fx Bank")]
public class FxBank : ScriptableObject
{
    [Header("Bus al que pertenece este Bank (Sfx, Ui, Voice, etc.)")]
    public AudioBus bus = AudioBus.Sfx;

    [Header("Listado de FX de esta subcategoría")]
    public List<FxEntry> entries = new();

    private Dictionary<string, FxEntry> _lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, FxEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.key))
                continue;

            if (!_lookup.ContainsKey(e.key))
                _lookup.Add(e.key, e);
        }
    }

    public FxEntry Get(string key)
    {
        if (_lookup == null || _lookup.Count == 0)
            BuildLookup();

        if (string.IsNullOrWhiteSpace(key))
            return null;

        _lookup.TryGetValue(key, out var entry);
        return entry;
    }

    public void Play2D(string key)
    {
        var entry = Get(key);
        if (entry == null || entry.clip == null) return;

        AudioManager.Instance.PlayClip2D(entry.clip, bus, key, entry.volume, entry.pitch, entry.loop);
    }

    public void Play3D(string key, Vector3 position)
    {
        var entry = Get(key);
        if (entry == null || entry.clip == null) return;

        AudioManager.Instance.PlayClip3D(entry.clip, bus, key, position, entry.volume, entry.pitch, entry.loop,
            entry.is3D ? entry.spatialBlend : 0f, entry.maxDistance);
    }

    public void Stop(string key)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoop(key);
        }
    }

    #region Gizmos

    public void DrawGizmo(Vector3 position, string key, Color color)
    {
        var entry = Get(key);
        if (entry == null) return;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(position, entry.maxDistance);

        Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
        Gizmos.DrawSphere(position, 0.2f);
    }

    #endregion
}

[Serializable]
public class FxEntry
{
    [Tooltip("Identificador de este sonido dentro del bank (ej: 'Menu_Open', 'Trap_Close')")]
    public string key;

    public AudioClip clip;

    [Header("Ajustes de reproducción")] [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.1f, 5f)] public float pitch = 1f;
    public bool loop;

    [Header("3D")] public bool is3D = true;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float maxDistance = 20f;
}