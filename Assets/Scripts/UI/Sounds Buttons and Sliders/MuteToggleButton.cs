/// <summary>
/// MuteToggleButton
/// Controla el icono de un botón de mute y mantiene un estado bool IsMuted.
/// - Al hacer click o Submit sobre el botón, alterna entre muteado / no muteado.
/// - Cambia el sprite base del Image según el estado.
/// - Expone un evento para que otro sistema (AudioManager, etc.) aplique el mute real.
/// No toca volúmenes directamente: una clase externa debe reaccionar a IsMuted.
/// </summary>

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public sealed class MuteToggleButton : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Sprite cuando NO está muteado (ej: Music).")]
    [SerializeField] private Sprite _spriteUnmuted;

    [Tooltip("Sprite cuando está muteado (ej: MusicMute).")]
    [SerializeField] private Sprite _spriteMuted;

    [Header("Estado inicial")]
    [SerializeField] private bool _startMuted = false;

    /// <summary>
    /// Estado actual del botón (true = muteado).
    /// </summary>
    public bool IsMuted { get; private set; }

    /// <summary>
    /// Evento C# cuando cambia el estado de mute.
    /// </summary>
    public event Action<bool> MutedChanged;

    [Header("Unity Event (opcional)")]
    [Tooltip("Se dispara cuando cambia el estado de mute (true = muteado).")]
    [SerializeField] private UnityEvent<bool> _onMutedChanged;

    private Button _button;
    private Image _image;
    private bool _initializedFromOutside;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image  = GetComponent<Image>();
    }
    
    private void Start()
    {
        // Si NADIE nos seteó desde afuera (AudioOptionsUI.SetMuted),
        // usamos el estado por defecto de inspector (_startMuted).
        if (!_initializedFromOutside)
        {
            IsMuted = _startMuted;
            ApplySprite();
        }
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        ToggleMute();
    }

    /// <summary>
    /// Alterna el estado de mute y actualiza el icono.
    /// </summary>
    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        _initializedFromOutside = true; // a partir de acá respetamos este valor
        ApplySprite();

        MutedChanged?.Invoke(IsMuted);
        _onMutedChanged?.Invoke(IsMuted);
    }

    private void ApplySprite()
    {
        if (_image == null) return;

        if (IsMuted)
        {
            if (_spriteMuted != null)
                _image.sprite = _spriteMuted;
        }
        else
        {
            if (_spriteUnmuted != null)
                _image.sprite = _spriteUnmuted;
        }
    }

    /// <summary>
    /// Permite setear el estado desde código externo sin disparar Toggle manual.
    /// </summary>
    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        _initializedFromOutside = true;
        ApplySprite();

        MutedChanged?.Invoke(IsMuted);
        _onMutedChanged?.Invoke(IsMuted);
    }
}