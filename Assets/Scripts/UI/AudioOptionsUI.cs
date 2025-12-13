using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AudioOptionsUI : MonoBehaviour
{
    [Header("Sliders (0..1)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider ambientSlider;
    [SerializeField] private Slider fxSlider;
    [SerializeField] private Slider uiSlider;

    [Header("Mute Buttons")]
    [SerializeField] private MuteToggleButton masterMuteButton;
    [SerializeField] private MuteToggleButton musicMuteButton;
    [SerializeField] private MuteToggleButton ambientMuteButton;
    [SerializeField] private MuteToggleButton sfxMuteButton;
    [SerializeField] private MuteToggleButton uiMuteButton;

    private bool _initializing;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioOptionsUI: No hay AudioManager.Instance en escena.");
            return;
        }

        _initializing = true;

        #region Sliders

        // ---------- MASTER ----------
        if (masterSlider)
        {
            float def = AudioManager.Instance.GetBusVolume(AudioBus.Master);
            float v = Save.GetVolume(VolumeSoundId.Master, def);
            masterSlider.value = v;
            ApplyMaster(v, save: false);
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        // ---------- MUSIC ----------
        if (musicSlider)
        {
            float def = AudioManager.Instance.GetBusVolume(AudioBus.Music);
            float v = Save.GetVolume(VolumeSoundId.Music, def);
            musicSlider.value = v;
            ApplyMusic(v, save: false);
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        // ---------- AMBIENT ----------
        if (ambientSlider)
        {
            float def = AudioManager.Instance.GetBusVolume(AudioBus.Ambient);
            float v = Save.GetVolume(VolumeSoundId.Ambient, def);
            ambientSlider.value = v;
            ApplyAmbient(v, save: false);
            ambientSlider.onValueChanged.AddListener(OnAmbientSliderChanged);
        }

        // ---------- FX ----------
        if (fxSlider)
        {
            float def = AudioManager.Instance.GetBusVolume(AudioBus.Sfx);
            float v = Save.GetVolume(VolumeFxId.Sfx, def);
            fxSlider.value = v;
            ApplyFx(v, save: false);
            fxSlider.onValueChanged.AddListener(OnFxSliderChanged);
        }

        // ---------- UI ----------
        if (uiSlider)
        {
            float def = AudioManager.Instance.GetBusVolume(AudioBus.UI);
            float v = Save.GetVolume(VolumeFxId.UI, def);
            uiSlider.value = v;
            ApplyUi(v, save: false);
            uiSlider.onValueChanged.AddListener(OnUiSliderChanged);
        }

        #endregion

        #region Buttons
        
        // MASTER
        if (masterMuteButton)
        {
            masterMuteButton.MutedChanged += OnMasterMutedChanged;

            bool muted = Save.GetMuted(VolumeSoundId.Master, false);
            masterMuteButton.SetMuted(muted, notify: false); 
            
            // aplicar al mixer en el arranque
            if (muted) AudioManager.Instance.SetBusVolume(AudioBus.Master, 0f);
        }

        // MUSIC
        if (musicMuteButton)
        {
            musicMuteButton.MutedChanged += OnMusicMutedChanged;

            bool muted = Save.GetMuted(VolumeSoundId.Music, false);
            musicMuteButton.SetMuted(muted, notify: false); 
            
            // aplicar al mixer en el arranque
            if (muted) AudioManager.Instance.SetBusVolume(AudioBus.Music, 0f);
        }

        // AMBIENT
        if (ambientMuteButton)
        {
            ambientMuteButton.MutedChanged += OnAmbientMutedChanged;

            bool muted = Save.GetMuted(VolumeSoundId.Ambient, false);
            ambientMuteButton.SetMuted(muted, notify: false); 
            
            // aplicar al mixer en el arranque
            if (muted) AudioManager.Instance.SetBusVolume(AudioBus.Ambient, 0f);
        }

        // SFX
        if (sfxMuteButton)
        {
            sfxMuteButton.MutedChanged += OnSfxMutedChanged;

            bool muted = Save.GetMuted(VolumeFxId.Sfx, false);
            sfxMuteButton.SetMuted(muted, notify: false); 
            
            // aplicar al mixer en el arranque
            if (muted) AudioManager.Instance.SetBusVolume(AudioBus.Sfx, 0f);
        }

        // UI
        if (uiMuteButton)
        {
            uiMuteButton.MutedChanged += OnUiMutedChanged;

            bool muted = Save.GetMuted(VolumeFxId.UI, false);
            uiMuteButton.SetMuted(muted, notify: false); 
            
            // aplicar al mixer en el arranque
            if (muted) AudioManager.Instance.SetBusVolume(AudioBus.UI, 0f);
        }

        #endregion


        _initializing = false;
    }

    #region Slider Callbacks

    private void OnMasterSliderChanged(float v)
    {
        if (_initializing) return;

        // Siempre guardamos el valor, aunque esté muteado
        Save.SetVolume(VolumeSoundId.Master, v);

        if (masterMuteButton && masterMuteButton.IsMuted) 
            return; // si está muteado, no tocamos el mixer; seguirá en 0

        ApplyMaster(v, save: false);
    }

    private void OnMusicSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeSoundId.Music, v);

        if (musicMuteButton && musicMuteButton.IsMuted) 
            return;

        ApplyMusic(v, save: false);
    }

    private void OnAmbientSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeSoundId.Ambient, v);

        if (ambientMuteButton && ambientMuteButton.IsMuted) 
            return;

        ApplyAmbient(v, save: false);
    }

    private void OnFxSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeFxId.Sfx, v);

        
        if (sfxMuteButton && sfxMuteButton.IsMuted) 
            return;

        ApplyFx(v, save: false);
    }

    private void OnUiSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeFxId.UI, v);
        
        if (uiMuteButton && uiMuteButton.IsMuted) 
            return;

        ApplyUi(v, save: false);
    }

    #endregion

    #region Mute Toggles
    
    private void OnMasterMutedChanged(bool muted)
    {
        if (AudioManager.Instance == null) return;

        Save.SetMuted(VolumeSoundId.Master, muted);

        if (muted)
        {
            AudioManager.Instance.SetBusVolume(AudioBus.Master, 0f);
        }
        else
        {
            float v = masterSlider ? masterSlider.value : 1f;
            ApplyMaster(v, save: false);
        }
    }

    private void OnMusicMutedChanged(bool muted)
    {
        if (AudioManager.Instance == null) return;

        Save.SetMuted(VolumeSoundId.Music, muted);

        if (muted)
            AudioManager.Instance.SetBusVolume(AudioBus.Music, 0f);
        else
        {
            float v = musicSlider ? musicSlider.value : 1f;
            ApplyMusic(v, save: false);
        }
    }

    private void OnAmbientMutedChanged(bool muted)
    {
        if (AudioManager.Instance == null) return;

        Save.SetMuted(VolumeSoundId.Ambient, muted);

        if (muted)
            AudioManager.Instance.SetBusVolume(AudioBus.Ambient, 0f);
        else
        {
            float v = ambientSlider ? ambientSlider.value : 1f;
            ApplyAmbient(v, save: false);
        }
    }

    private void OnSfxMutedChanged(bool muted)
    {
        if (AudioManager.Instance == null) return;

        Save.SetMuted(VolumeFxId.Sfx, muted);

        if (muted)
            AudioManager.Instance.SetBusVolume(AudioBus.Sfx, 0f);
        else
        {
            float v = fxSlider ? fxSlider.value : 1f;
            ApplyFx(v, save: false);
        }
    }

    private void OnUiMutedChanged(bool muted)
    {
        if (AudioManager.Instance == null) return;

        Save.SetMuted(VolumeFxId.UI, muted);

        if (muted)
            AudioManager.Instance.SetBusVolume(AudioBus.UI, 0f);
        else
        {
            float v = uiSlider ? uiSlider.value : 1f;
            ApplyUi(v, save: false);
        }
    }

    #endregion

    #region Aplicadores

    private void ApplyMaster(float v01, bool save)
    {
        AudioManager.Instance.SetBusVolume(AudioBus.Master, v01);
        if (save) Save.SetVolume(VolumeSoundId.Master, v01);
    }

    private void ApplyMusic(float v01, bool save)
    {
        AudioManager.Instance.SetBusVolume(AudioBus.Music, v01);
        if (save) Save.SetVolume(VolumeSoundId.Music, v01);
    }

    private void ApplyAmbient(float v01, bool save)
    {
        AudioManager.Instance.SetBusVolume(AudioBus.Ambient, v01);
        if (save) Save.SetVolume(VolumeSoundId.Ambient, v01);
    }

    private void ApplyFx(float v01, bool save)
    {
        AudioManager.Instance.SetBusVolume(AudioBus.Sfx, v01);
        if (save) Save.SetVolume(VolumeFxId.Sfx, v01);
    }

    private void ApplyUi(float v01, bool save)
    {
        AudioManager.Instance.SetBusVolume(AudioBus.UI, v01);
        if (save) Save.SetVolume(VolumeFxId.UI, v01);
    }

    #endregion
    
    private void OnDestroy()
    {
        if (masterSlider) masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        if (musicSlider)  musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (ambientSlider) ambientSlider.onValueChanged.RemoveListener(OnAmbientSliderChanged);
        if (fxSlider) fxSlider.onValueChanged.RemoveListener(OnFxSliderChanged);
        if (uiSlider) uiSlider.onValueChanged.RemoveListener(OnUiSliderChanged);

        if (masterMuteButton) masterMuteButton.MutedChanged -= OnMasterMutedChanged;
        if (musicMuteButton)  musicMuteButton.MutedChanged  -= OnMusicMutedChanged;
        if (ambientMuteButton) ambientMuteButton.MutedChanged -= OnAmbientMutedChanged;
        if (sfxMuteButton) sfxMuteButton.MutedChanged -= OnSfxMutedChanged;
        if (uiMuteButton) uiMuteButton.MutedChanged -= OnUiMutedChanged;
    }

}
