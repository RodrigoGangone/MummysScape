using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// PlayerView
/// Solo visual: Animator/FX/UI. Sin reglas de juego.
/// </summary>
public sealed class PlayerView : MonoBehaviour
{
    [Header("Anim & FX")]
    [SerializeField] private Animator _anim;
    [SerializeField] private ParticleSystem _shootFX;
    [SerializeField] private ParticleSystem _smashFX;
    [SerializeField] private GameObject _decal;
    [SerializeField] private GameObject _rangeIndicator;
    
    [Header("UI (opcional)")]
    [SerializeField] private Image _headTimerFill; 
    [SerializeField] private Sprite _spriteHead;
    [SerializeField] private Sprite _spriteNormalOrSmall;

    public GameObject Decal => _decal;
    public GameObject RangeIndicator => _rangeIndicator;

    public void SetMoveSpeedVisual(float normalized)
    {
        if (_anim) _anim.SetFloat("Speed", normalized);
    }

    public void PlayShoot()
    {
        _anim?.SetTrigger("Shoot");
        if (_shootFX && !_shootFX.isPlaying) _shootFX.Play();
    }

    public void PlaySmash()
    {
        _anim?.SetTrigger("Smash");
        if (_smashFX && !_smashFX.isPlaying) _smashFX.Play();
    }

    public void SetHeadTimerSprite(bool isHead)
    {
        if (_headTimerFill == null) return;
        _headTimerFill.sprite = isHead ? _spriteHead : _spriteNormalOrSmall;
        _headTimerFill.fillAmount = 1f;
    }

    public void UpdateHeadTimer01(float n01)
    {
        if (_headTimerFill) _headTimerFill.fillAmount = Mathf.Clamp01(n01);
    }
}