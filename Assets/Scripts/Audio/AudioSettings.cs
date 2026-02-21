using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary> 
/// Configuración de Buses: Define el mapeo entre la lógica del juego (AudioBus) y los grupos físicos 
/// del AudioMixer, estableciendo volúmenes predeterminados y parámetros de control. 
/// </summary>

[Serializable]
public struct BusConfig
{
    public AudioBus bus;

    [Range(0f, 1f)]
    public float defaultVolume;

    [Tooltip("Nombre del parámetro expuesto en el AudioMixer (ej: 'MusicVol')")]
    public string mixerParameter;

    [Tooltip("AudioMixerGroup al que van las fuentes de este bus")]
    public AudioMixerGroup mixerGroup;
}

[CreateAssetMenu(fileName = "AudioSettings", menuName = "Audio/Audio Settings")]
public class AudioSettings : ScriptableObject
{
    public AudioMixer mixer;
    public List<BusConfig> buses = new();
}