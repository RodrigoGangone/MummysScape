using System.Collections.Generic;
using UnityEngine;

public class AudioSourceFactory : MonoBehaviour
{
    public static AudioSourceFactory Instance { get; private set; } 

    [SerializeField] private AudioSource audioSourcePrefab;
    private Pool<AudioSource> audioSourcePool;
    [SerializeField] private int initialAmount = 20;
    private List<AudioSource> activeAudioSources; // Lista para rastrear los AudioSource activos

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        activeAudioSources = new List<AudioSource>();
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
        activeAudioSources.Add(source); 
    }

    private void TurnOffAudioSource(AudioSource source)
    {
        source.Stop();
        source.clip = null; // Limpia el clip para evitar confusiones
        source.gameObject.SetActive(false);
        activeAudioSources.Remove(source); 
    }

    public AudioSource GetAudioSourceFromPool()
    {
        return audioSourcePool.GetObject();
    }

    public void ReturnAudioSourceToPool(AudioSource source)
    {
        audioSourcePool.ReturnObject(source);
    }
    
    // Obtiene todos los AudioSource actualmente en uso
    public List<AudioSource> GetActiveAudioSources()
    {
        return new List<AudioSource>(activeAudioSources); // Devuelve una copia de la lista de activos
    }
}