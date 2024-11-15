using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static Utils;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer _audioMixer;
    
    [SerializeField] private Button _btnMusic;
    [SerializeField] private Button _btnSFX;

    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    
    void Start()
    {
        _btnMusic.onClick.AddListener(ToggleMusic);
        _btnSFX.onClick.AddListener(ToggleSFX);

        if (PlayerPrefs.HasKey(MUSIC_VOLUME) && PlayerPrefs.HasKey(SFX_VOLUME))
        {
            LoadVolume();
        }
        else
        {
            SetMusicVolume();
            SetSFXVolume();
        }
    }
    
    private void ToggleMusic()
    {
        AudioManager.Instance.ToogleMusic();
    }
    
    private void ToggleSFX()
    {
        AudioManager.Instance.ToogleSFX();
    }

    private void LoadVolume()
    {
        _musicSlider.value = PlayerPrefs.GetFloat(MUSIC_VOLUME);
        _sfxSlider.value = PlayerPrefs.GetFloat(SFX_VOLUME);
        SetMusicVolume();
        SetSFXVolume();
    }

    public void SetMusicVolume()
    {
        float volume = _musicSlider.value;
        _audioMixer.SetFloat(AUDIO_MIXER_MUSIC, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(MUSIC_VOLUME, volume);
    }
    
    public void SetSFXVolume()
    {
        float volume = _sfxSlider.value;
        _audioMixer.SetFloat(AUDIO_MIXER_SFX, Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(SFX_VOLUME, volume);
    }
}
