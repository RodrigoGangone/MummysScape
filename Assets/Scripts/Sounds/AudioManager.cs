using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    public Sound[] musicSounds, sfxSounds;
    public AudioSource musicSource, sfxSource;

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
        
    }

    public void PlayMusic(string name)
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

    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);

        if (s != null)
        {
            sfxSource.clip = s.clip;
            sfxSource.loop = s.loop;
            sfxSource.Play();
        }
        else Debug.Log("Sfx Not Found");
    }
    
    public void StopMusic(string name)
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
    
    public void StopSFX(string name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);

        if (s != null && sfxSource.isPlaying && sfxSource.clip == s.clip)
        {
            sfxSource.Stop();
            Debug.Log($"Stopped SFX: {name}");
        }
        else
        {
            Debug.Log($"SFX '{name}' is not currently playing.");
        }
    }

    public void ToogleMusic()
    {
        musicSource.mute = !musicSource.mute;
    }
    
    public void ToogleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
    }
}