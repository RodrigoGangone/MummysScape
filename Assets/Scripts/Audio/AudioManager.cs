using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private AudioSettings _settings;

    [Header("AudioSources 2D")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _ambienceSource;
    [SerializeField] private AudioSource _uiSource;
    [SerializeField] private AudioSource _sfx2DSource;
    [SerializeField] private AudioSource _voiceSource;

    private Dictionary<AudioBus, BusConfig> _busLookup;
    private readonly Dictionary<AudioBus, float> _busVolumes = new();

    private void Awake() 
    {
        // Singleton simple
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitBuses();
        ApplyDefaultVolumes();
        Assign2DSourcesToMixerGroups();
    }

    #region Buses & Volumen

    private void InitBuses()
    {
        _busLookup = new Dictionary<AudioBus, BusConfig>();

        if (_settings == null)
        {
            Debug.LogWarning("AudioManager: No hay AudioSettings asignado.");
            return;
        }

        foreach (var b in _settings.buses)
        {
            _busLookup[b.bus] = b;
        }
    }

    private void ApplyDefaultVolumes()
    {
        if (_settings == null || _settings.mixer == null)
            return;

        foreach (var kvp in _busLookup)
        {
            SetBusVolume(kvp.Key, kvp.Value.defaultVolume);
        }
    }

    /// <summary>
    /// Setea el volumen de un bus (0..1) y lo mapea a dB en el AudioMixer.
    /// </summary>
    public void SetBusVolume(AudioBus bus, float volume01)
    {
        if (_settings == null || _settings.mixer == null)
            return;

        if (!_busLookup.TryGetValue(bus, out var config))
            return;

        volume01 = Mathf.Clamp01(volume01);
        _busVolumes[bus] = volume01;

        if (!string.IsNullOrEmpty(config.mixerParameter))
        {
            // 0..1 -> dB (aprox -80 a 0)
            float dB = volume01 > 0.0001f ? Mathf.Log10(volume01) * 20f : -80f;
            _settings.mixer.SetFloat(config.mixerParameter, dB);
        }
    }

    public float GetBusVolume(AudioBus bus)
    {
        return _busVolumes.TryGetValue(bus, out var v) ? v : 1f;
    }

    public AudioMixerGroup GetMixerGroup(AudioBus bus)
    {
        if (_busLookup == null)
            return null;

        if (_busLookup.TryGetValue(bus, out var config))
            return config.mixerGroup;

        return null;
    }
    
    #endregion

    #region Play 2D

    /// <summary>
    /// Reproduce un clip 2D en el bus indicado.
    /// </summary>
    public void PlayClip2D(AudioClip clip, AudioBus bus, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
            return;

        AudioSource src = null;

        switch (bus)
        {
            case AudioBus.Music:
                src = _musicSource;
                break;
            case AudioBus.Ambient:
                src = _ambienceSource;
                break;
            case AudioBus.UI:
                src = _uiSource;
                break;
            case AudioBus.Voice:
                src = _voiceSource;
                break;
            default:
                src = _sfx2DSource;
                break;
        }

        if (src == null) 
        {
            Debug.LogWarning($"AudioManager: No hay AudioSource asignado para el bus {bus}");
            return;
        }

        src.pitch = pitch;

        // Para música/ambiente suele ser mejor asignar clip y Play normal.
        if (bus == AudioBus.Music || bus == AudioBus.Ambient)
        {
            src.clip = clip;
            src.loop = true; // ajustalo según tu caso
            src.volume = volume;
            src.Play();
        }
        else
        {
            src.PlayOneShot(clip, volume);
        }
    }

    #endregion

    #region Play 3D

    /// <summary>
    /// Crea un AudioSource 3D temporal en la posición indicada y lo destruye al terminar.
    /// </summary>
    public void PlayClip3D(
        AudioClip clip,
        AudioBus bus,
        Vector3 position,
        float volume = 1f,
        float pitch = 1f,
        float spatialBlend = 1f,
        float maxDistance = 20f
    )
    {
        if (clip == null)
            return;

        var go = new GameObject($"OneShot3D_{clip.name}");
        go.transform.position = position;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = spatialBlend;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Linear;

        // Asignar el mixer group según el bus
        if (_busLookup != null && _busLookup.TryGetValue(bus, out var config) && config.mixerGroup != null)
        {
            src.outputAudioMixerGroup = config.mixerGroup;
        } 

        src.Play();
        Destroy(go, clip.length + 0.5f);
        
        Debug.Log("PlaySoundFx3D");
    }

    #endregion
    
    private void Assign2DSourcesToMixerGroups()
    {
        AssignSourceToBus(_musicSource,    AudioBus.Music);
        AssignSourceToBus(_ambienceSource, AudioBus.Ambient);
        AssignSourceToBus(_uiSource,       AudioBus.UI);
        AssignSourceToBus(_sfx2DSource,    AudioBus.Sfx);
        AssignSourceToBus(_voiceSource,    AudioBus.Voice);
    }

    private void AssignSourceToBus(AudioSource src, AudioBus bus)
    {
        if (src == null)
            return;

        var group = GetMixerGroup(bus);
        if (group != null)
            src.outputAudioMixerGroup = group;
    }

}
