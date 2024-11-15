using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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
            sfxSource.clip = s.clip;
            sfxSource.loop = s.loop;
            sfxSource.Play();
        }
        else Debug.Log("Sfx Not Found");
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