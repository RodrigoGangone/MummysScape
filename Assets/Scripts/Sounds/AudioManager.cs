using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using static Utils;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioMixer audioMixer;
    public Sound[] musicSounds, sfxSounds;
    public AudioSource musicSource;

    private bool isSFXMuted;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        isSFXMuted = PlayerPrefs.GetFloat(SFX_VOLUME).Equals(-80);
        
        PlayMusicBySceneIndex();
    }

    private void PlayMusicBySceneIndex()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        switch (sceneIndex)
        {
            case 0:
                NameSounds randomSound = (Random.value > 0.5f) ? NameSounds.MainMenu1 : NameSounds.MainMenu2;
                PlayMusic(randomSound);
                break;
            case 1:
                PlayMusic(NameSounds.Lvl1_1); 
                break;
            case 2:
                PlayMusic(NameSounds.Lvl1_1); 
                break;
            default:
                Debug.Log("No music assigned for this scene index.");
                break;
        }
    }

    public void PlayMusic(NameSounds name)
    {
        Sound s = Array.Find(musicSounds, x => x.name == name);

        if (s != null)
        {
            musicSource.clip = s.clip;
            musicSource.loop = s.loop;
            musicSource.Play();
        }
        else Debug.Log("Music Not Found");
    }

    public void PlaySFX(NameSounds name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);

        if (s != null)
        {
            AudioSource audioSource = AudioSourceFactory.Instance.GetAudioSourceFromPool();

            audioSource.clip = s.clip;
            audioSource.loop = s.loop;
            audioSource.Play();

            // Devuelve el AudioSource al pool si no es loop cuando termina de reproducir
            if (!s.loop) 
                StartCoroutine(ReturnAudioSourceToPool(audioSource, s.clip.length));
        }
        else
        {
            Debug.LogError($"SFX '{name}' not found!");
        }
    }
    
    public void StopMusic(NameSounds name)
    {
        Sound s = Array.Find(musicSounds, x => x.name == name);

        if (s != null && musicSource.isPlaying && musicSource.clip == s.clip)
        {
            musicSource.Stop();
            Debug.Log($"Stopped music: {name}");
        }
        else
        {
            Debug.Log($"Music '{name}' is not currently playing.");
        }
    }
    
    public void StopSFX(NameSounds name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);

        if (s != null)
        {
            foreach (Transform child in AudioSourceFactory.Instance.transform)
            {
                AudioSource source = child.GetComponent<AudioSource>();
                if (source != null && source.isPlaying && source.clip == s.clip)
                {
                    source.Stop();
                    AudioSourceFactory.Instance.ReturnAudioSourceToPool(source);
                    Debug.Log($"Stopped and returned SFX: {name}");
                    return;
                }
            }
            Debug.Log($"SFX '{name}' is not currently playing.");
        }
        else
        {
            Debug.LogError($"SFX '{name}' not found!");
        }
    }

    public void ToogleMusic()
    {
        musicSource.mute = !musicSource.mute;
    }
    
    public void ToogleSFX()
    {
        isSFXMuted = !isSFXMuted;
        
        // AudioSources activos desde la pool
        List<AudioSource> activeAudioSources = AudioSourceFactory.Instance.GetActiveAudioSources();

        //Los muteo o desmuteo a todos
        foreach (AudioSource source in activeAudioSources)
        {
            source.mute = isSFXMuted;
        }

        //TODO: En caso de que no funcione el bloque de arriba usar este.
        /*
        //Si no esta muteado: pone el volumen en -80
        //Si esta muteado: poner el volumen en lo que dice las pref
         float targetVolume = isSFXMuted ? -80 : PlayerPrefs.GetFloat(SFX_VOLUME);

        // Ajusta el volumen del grupo "SFX" en el AudioMixer.
        audioMixer.SetFloat(AUDIO_MIXER_SFX, targetVolume);

        Debug.Log($"SFX Group is now {(isSFXMuted ? "muted" : "unmuted")}. Current volume: {targetVolume}");*/
    }
    
    private IEnumerator ReturnAudioSourceToPool(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioSourceFactory.Instance.ReturnAudioSourceToPool(audioSource);
    }
}