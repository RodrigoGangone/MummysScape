using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbientLayer2D : MonoBehaviour
{
    [Header("Banco de ambiente (FxBank)")]
    [SerializeField] private FxBank ambienceBank;

    [SerializeField] private string key;          // ej: "Wind", "Sand", "TempleLow"
    [SerializeField] private float defaultVolume = 1f;
    [SerializeField] private float defaultFadeTime = 1f;

    private AudioSource _src;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = true;
        _src.spatialBlend = 0f; // 2D

        // Enchufar al bus Ambient del mixer
        if (AudioManager.Instance != null)
        {
            var group = AudioManager.Instance.GetMixerGroup(AudioBus.Ambient);
            if (group != null)
                _src.outputAudioMixerGroup = group;
        }
    }

    private void Start()
    {
        InitLayer();
    }

    private void InitLayer()
    {
        if (ambienceBank == null || string.IsNullOrEmpty(key))
            return;

        var entry = ambienceBank.Get(key);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"AmbienceLayer2D ({name}): key '{key}' no encontrada en bank '{ambienceBank?.name}'.");
            return;
        }

        _src.clip = entry.clip;
        _src.pitch = entry.pitch;
        _src.volume = 0f;
        _src.Play();

        SetTargetVolume(defaultVolume, defaultFadeTime);
    }

    public void SetTargetVolume(float target, float fadeTime = -1f)
    {
        if (fadeTime < 0f)
            fadeTime = defaultFadeTime;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeTo(target, fadeTime));
    }

    private IEnumerator FadeTo(float target, float time)
    {
        float start = _src.volume;
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float k = time > 0f ? t / time : 1f;
            _src.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }

        _src.volume = target;
        _fadeCoroutine = null;
    }
}
