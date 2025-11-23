using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Controla la música 2D con prioridad y crossfade.
/// Usa un FxBank con bus = Music (is3D = false).
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Banco de música (FxBank)")] [SerializeField]
    private FxBank musicBank;

    [Header("Config")] [SerializeField] private float defaultFadeTime = 1.5f;
    [SerializeField] private string startKey = ""; // ej: "MainTheme"

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _currentSource;
    private AudioSource _nextSource;

    private string _currentKey;
    private int _currentPriority = int.MinValue;
    private Coroutine _fadeCoroutine;

    private float _currentTargetVolume = 1f;
    private float _nextTargetVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);

        Instance = this;

        CreateSources();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(startKey))
            PlayMusic(startKey, priority: 0, fadeTime: 0f);
    }

    private void CreateSources()
    {
        _sourceA = gameObject.AddComponent<AudioSource>();
        _sourceB = gameObject.AddComponent<AudioSource>();

        SetupSource(_sourceA);
        SetupSource(_sourceB);

        _currentSource = _sourceA;
        _nextSource = _sourceB;

        // Enchufar al bus Music del mixer
        if (AudioManager.Instance != null)
        {
            var group = AudioManager.Instance.GetMixerGroup(AudioBus.Music);
            if (group != null)
            {
                _sourceA.outputAudioMixerGroup = group;
                _sourceB.outputAudioMixerGroup = group;
                Debug.Log($"MusicManager: usando mixer group '{group.name}' para música");
            }
            else
            {
                Debug.LogWarning("MusicManager: NO encontró mixer group para AudioBus.Music");
            }
        
    }
    }

    private void SetupSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f; // 2D
        src.volume = 0f;
    }

    /// <summary>
    /// Reproduce un tema por key con prioridad y crossfade.
    /// </summary>
    public void PlayMusic(string key, int priority = 0, float fadeTime = -1f, bool forceRestart = false)
    {
        if (musicBank == null)
        {
            Debug.LogWarning("MusicManager: no hay MusicBank asignado.");
            return;
        }

        var entry = musicBank.Get(key);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"MusicManager: key '{key}' no encontrada en bank '{musicBank.name}'.");
            return;
        }

        if (fadeTime < 0f)
            fadeTime = defaultFadeTime;

        // Si la nueva prioridad es menor a la actual, ignoramos
        if (!forceRestart && priority < _currentPriority)
            return;

        // Si es el mismo tema y no queremos reiniciar, no hacemos nada
        if (!forceRestart && _currentSource != null && _currentSource.clip == entry.clip)
            return;

        _currentPriority = priority;
        _currentKey = key;

        // Preparar el source de destino
        _nextSource.clip = entry.clip;
        _nextSource.pitch = entry.pitch;

        // El volumen lo vamos a animar desde 0 hasta entry.volume en el crossfade
        _nextSource.volume = 0f;
        _nextTargetVolume = entry.volume;
        _currentTargetVolume = _currentSource != null ? _currentSource.volume : 1f;

        _nextSource.Play();

        // Lanzar crossfade
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(CrossfadeCoroutine(fadeTime));
    }

    /// <summary>
    /// Apaga la música actual con fade.
    /// </summary>
    public void StopMusic(float fadeTime = -1f)
    {
        if (fadeTime < 0f)
            fadeTime = defaultFadeTime;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeOutAllCoroutine(fadeTime));
        _currentPriority = int.MinValue;
        _currentKey = null;
    }

    private IEnumerator CrossfadeCoroutine(float duration)
    {
        float t = 0f;

        float startVolCurrent = _currentSource.volume;
        float targetVolCurrent = 0f;                 // el actual baja a 0

        float startVolNext = 0f;                     // el nuevo arranca en 0
        float targetVolNext = _nextTargetVolume;     // y sube al volumen del Bank

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? t / duration : 1f;
            k = Mathf.Clamp01(k);

            _currentSource.volume = Mathf.Lerp(startVolCurrent, targetVolCurrent, k);
            _nextSource.volume    = Mathf.Lerp(startVolNext,    targetVolNext,   k);

            yield return null;
        }

        _currentSource.volume = targetVolCurrent;
        _currentSource.Stop();

        _nextSource.volume     = targetVolNext;
        _currentTargetVolume   = targetVolNext;   // por si después querés usarlo

        // Swap referencias
        (_currentSource, _nextSource) = (_nextSource, _currentSource);

        _fadeCoroutine = null;
    }


    private IEnumerator FadeOutAllCoroutine(float duration)
    {
        float t = 0f;
        float startA = _sourceA.volume;
        float startB = _sourceB.volume;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? t / duration : 1f;
            k = Mathf.Clamp01(k);

            _sourceA.volume = Mathf.Lerp(startA, 0f, k);
            _sourceB.volume = Mathf.Lerp(startB, 0f, k);

            yield return null;
        }

        _sourceA.volume = 0f;
        _sourceA.Stop();
        _sourceB.volume = 0f;
        _sourceB.Stop();

        _fadeCoroutine = null;
    }
}