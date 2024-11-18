using UnityEngine;

public class AudioSourceFactory : MonoBehaviour
{
    public static AudioSourceFactory Instance { get; private set; } 

    [SerializeField] private AudioSource audioSourcePrefab;
    private Pool<AudioSource> audioSourcePool;
    [SerializeField] private int initialAmount = 10;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSourcePool = new Pool<AudioSource>(CreateAudioSource, TurnOnAudioSource, TurnOffAudioSource, initialAmount);
    }

    private AudioSource CreateAudioSource()
    {
        AudioSource newSource = Instantiate(audioSourcePrefab, transform);
        newSource.gameObject.SetActive(false);
        return newSource;
    }

    private void TurnOnAudioSource(AudioSource source)
    {
        source.gameObject.SetActive(true);
    }

    private void TurnOffAudioSource(AudioSource source)
    {
        source.Stop();
        source.clip = null; // Limpia el clip para evitar confusiones
        source.gameObject.SetActive(false);
    }

    public AudioSource GetAudioSourceFromPool()
    {
        return audioSourcePool.GetObject();
    }

    public void ReturnAudioSourceToPool(AudioSource source)
    {
        audioSourcePool.ReturnObject(source);
    }
}