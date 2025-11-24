using UnityEngine;
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
    [SerializeField] private Button masterMuteButton;
    [SerializeField] private Button musicMuteButton;
    [SerializeField] private Button ambientMuteButton;
    [SerializeField] private Button fxMuteButton;
    [SerializeField] private Button uiMuteButton;

    // Estados de mute internos (no mueven el slider)
    private bool _masterMuted;
    private bool _musicMuted;
    private bool _ambientMuted;
    private bool _fxMuted;
    private bool _uiMuted;

    private bool _initializing;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioOptionsUI: No hay AudioManager.Instance en escena.");
            return;
        }

        _initializing = true;

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

        // ---------- MUTE BUTTONS ----------
        if (masterMuteButton)  masterMuteButton.onClick.AddListener(ToggleMasterMute);
        if (musicMuteButton)   musicMuteButton.onClick.AddListener(ToggleMusicMute);
        if (ambientMuteButton) ambientMuteButton.onClick.AddListener(ToggleAmbientMute);
        if (fxMuteButton)      fxMuteButton.onClick.AddListener(ToggleFxMute);
        if (uiMuteButton)      uiMuteButton.onClick.AddListener(ToggleUiMute);

        _initializing = false;
    }

    #region Slider Callbacks

    private void OnMasterSliderChanged(float v)
    {
        if (_initializing) return;

        // Siempre guardamos el valor, aunque esté muteado
        Save.SetVolume(VolumeSoundId.Master, v);

        if (_masterMuted)
            return; // si está muteado, no tocamos el mixer; seguirá en 0

        ApplyMaster(v, save: false);
    }

    private void OnMusicSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeSoundId.Music, v);

        if (_musicMuted)
            return;

        ApplyMusic(v, save: false);
    }

    private void OnAmbientSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeSoundId.Ambient, v);

        if (_ambientMuted)
            return;

        ApplyAmbient(v, save: false);
    }

    private void OnFxSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeFxId.Sfx, v);

        if (_fxMuted)
            return;

        ApplyFx(v, save: false);
    }

    private void OnUiSliderChanged(float v)
    {
        if (_initializing) return;

        Save.SetVolume(VolumeFxId.UI, v);

        if (_uiMuted)
            return;

        ApplyUi(v, save: false);
    }

    #endregion

    #region Mute Toggles

    private void ToggleMasterMute()
    {
        _masterMuted = !_masterMuted;

        if (_masterMuted)
        {
            // mute: mandamos el bus a 0, pero NO tocamos el slider
            AudioManager.Instance.SetBusVolume(AudioBus.Master, 0f);
        }
        else
        {
            // unmute: aplicamos el valor actual del slider
            float v = masterSlider ? masterSlider.value : 1f;
            ApplyMaster(v, save: false);
        }

        // TODO: acá podés cambiar ícono del botón (mute/unmute) si querés
    }

    private void ToggleMusicMute()
    {
        _musicMuted = !_musicMuted;

        if (_musicMuted)
        {
            AudioManager.Instance.SetBusVolume(AudioBus.Music, 0f);
        }
        else
        {
            float v = musicSlider ? musicSlider.value : 1f;
            ApplyMusic(v, save: false);
        }
    }

    private void ToggleAmbientMute()
    {
        _ambientMuted = !_ambientMuted;

        if (_ambientMuted)
        {
            AudioManager.Instance.SetBusVolume(AudioBus.Ambient, 0f);
        }
        else
        {
            float v = ambientSlider ? ambientSlider.value : 1f;
            ApplyAmbient(v, save: false);
        }
    }

    private void ToggleFxMute()
    {
        _fxMuted = !_fxMuted;

        if (_fxMuted)
        {
            AudioManager.Instance.SetBusVolume(AudioBus.Sfx, 0f);
        }
        else
        {
            float v = fxSlider ? fxSlider.value : 1f;
            ApplyFx(v, save: false);
        }
    }

    private void ToggleUiMute()
    {
        _uiMuted = !_uiMuted;

        if (_uiMuted)
        {
            AudioManager.Instance.SetBusVolume(AudioBus.UI, 0f);
        }
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
}
