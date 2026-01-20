using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static VolumeSoundId;
using static VolumeFxId;
using static Utils; // Asumo que todavía necesitas esto para las constantes del Mixer

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

        // MODIFICADO:
        // Ya no es necesario chequear con PlayerPrefs.HasKey.
        // Tu nuevo sistema Save.GetVolume() se encarga de los valores por defecto.
        LoadVolume();
    }
    
    private void ToggleMusic()
    {
        //AudioManagerr.Instance.ToogleMusic();
    }
    
    private void ToggleSFX()
    {
       // AudioManagerr.Instance.ToogleSFX();
    }

    private void LoadVolume()
    {
        // Esto ya estaba usando tu nuevo sistema, lo cual es correcto.
        _musicSlider.value = Save.GetVolume(Music);
        _sfxSlider.value = Save.GetVolume(Sfx);
        
        // Aplica los valores cargados al mixer
        SetMusicVolume();
        SetSFXVolume();
    }

    public void SetMusicVolume()
    {
        float volume = _musicSlider.value;
        
        // Esto sigue igual, asumiendo que AUDIO_MIXER_MUSIC viene de Utils
        _audioMixer.SetFloat(AUDIO_MIXER_MUSIC, Mathf.Log10(volume) * 20);
        
        // MODIFICADO: Usar el nuevo sistema Save
        // PlayerPrefs.SetFloat(MUSIC_VOLUME, volume); // <-- Línea anterior
        Save.SetVolume(Music, volume); // <-- Nueva línea
    }
    
    public void SetSFXVolume()
    {
        float volume = _sfxSlider.value;
        
        // Esto sigue igual, asumiendo que AUDIO_MIXER_SFX viene de Utils
        _audioMixer.SetFloat(AUDIO_MIXER_SFX, Mathf.Log10(volume) * 20);
        
        // MODIFICADO: Usar el nuevo sistema Save
        // PlayerPrefs.SetFloat(SFX_VOLUME, volume); // <-- Línea anterior
        Save.SetVolume(Sfx, volume); // <-- Nueva línea
    }
}