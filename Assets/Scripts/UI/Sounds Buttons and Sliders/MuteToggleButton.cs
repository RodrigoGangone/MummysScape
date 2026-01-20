/// <summary>
/// MuteToggleButton
/// Controla el ícono de un botón de mute y mantiene un estado bool IsMuted.
/// - Al hacer click / submit (Button) alterna muteado / no muteado.
/// - Cambia el sprite de un Image objetivo (puede ser el del botón o un hijo).
/// - Expone eventos para que un sistema externo (AudioOptionsUI) aplique el mute real y guarde prefs.
/// </summary>

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class MuteToggleButton : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Image que cambia de sprite (si tu ícono es un hijo, arrastralo acá). Si está vacío, usa el Image del mismo GO.")]
    [SerializeField] private Image _targetImage;

    [Header("Sprites")]
    [Tooltip("Sprite cuando NO está muteado.")]
    [SerializeField] private Sprite _spriteUnmuted;

    [Tooltip("Sprite cuando está muteado.")]
    [SerializeField] private Sprite _spriteMuted;

    [Header("Estado inicial (solo si nadie llama SetMuted desde afuera)")]
    [SerializeField] private bool _startMuted;

    [Header("Compatibilidad UI")]
    [Tooltip("Si el Button usa Transition=SpriteSwap y el target es el mismo Graphic, forzamos que todos los estados usen el mismo sprite.")]
    [SerializeField] private bool _forceSameSpriteOnSpriteSwap = true;

    public bool IsMuted { get; private set; }

    public event Action<bool> MutedChanged;

    [SerializeField] private UnityEvent<bool> _onMutedChanged;

    private Button _button;
    private bool _initializedFromOutside;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_targetImage == null) _targetImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (!_initializedFromOutside)
            SetMuted(_startMuted, notify: false);
    }

    private void OnEnable()  => _button.onClick.AddListener(OnClick);
    private void OnDisable() => _button.onClick.RemoveListener(OnClick);

    private void OnClick() => ToggleMute();

    public void ToggleMute() => SetMuted(!IsMuted, notify: true);

    public void SetMuted(bool muted, bool notify = true)
    {
        IsMuted = muted;
        _initializedFromOutside = true;

        ApplySprite();

        if (!notify) return;
        MutedChanged?.Invoke(IsMuted);
        _onMutedChanged?.Invoke(IsMuted);
    }

    private void ApplySprite()
    {
        if (_targetImage == null) return;

        var sprite = IsMuted ? _spriteMuted : _spriteUnmuted;
        if (sprite != null)
            _targetImage.sprite = sprite;

        // Evita que SpriteSwap te lo pise cuando el botón está seleccionado/highlighted
        if (!_forceSameSpriteOnSpriteSwap) return;
        if (_button == null) return;
        if (_button.transition != Selectable.Transition.SpriteSwap) return;
        if (_button.targetGraphic != _targetImage) return;

        var st = _button.spriteState;
        st.highlightedSprite = sprite;
        st.pressedSprite     = sprite;
        st.selectedSprite    = sprite;
        st.disabledSprite    = sprite;
        _button.spriteState  = st;
    }
}