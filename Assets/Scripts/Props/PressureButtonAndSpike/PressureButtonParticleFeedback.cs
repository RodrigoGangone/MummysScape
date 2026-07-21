using System;
using UnityEngine;

/// <summary>
/// Reproduce las partículas configuradas cada vez que el botón entra en HalfPressed o FullyPressed.
/// Se suscribe al estado efectivo del botón y reinicia los sistemas para garantizar un nuevo efecto
/// incluso cuando la transición ocurre antes de que haya terminado la reproducción anterior.
/// </summary>
[DisallowMultipleComponent]
public sealed class PressureButtonParticleFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PressureButtonStateResolver _stateResolver;
    [SerializeField] private Transform _particlesRoot;

    [Header("States")]
    [SerializeField] private bool _playOnHalfPressed = true;
    [SerializeField] private bool _playOnFullyPressed = true;

    [Header("Playback")]
    [Tooltip("Reinicia y limpia las partículas antes de reproducirlas nuevamente.")]
    [SerializeField] private bool _restartIfAlreadyPlaying = true;

    private ParticleSystem[] _particleSystems = Array.Empty<ParticleSystem>();

    private void Awake()
    {
        CacheParticleSystems();
    }

    private void OnEnable()
    {
        if (_stateResolver == null)
        {
            Debug.LogError(
                $"{nameof(PressureButtonParticleFeedback)} requiere una referencia a " +
                $"{nameof(PressureButtonStateResolver)}.",
                this);

            return;
        }

        _stateResolver.EffectiveStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (_stateResolver != null)
        {
            _stateResolver.EffectiveStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(PressureButtonState state)
    {
        bool shouldPlay =
            (_playOnHalfPressed && state == PressureButtonState.HalfPressed) ||
            (_playOnFullyPressed && state == PressureButtonState.FullyPressed);

        if (!shouldPlay)
        {
            return;
        }

        PlayParticles();
    }

    private void PlayParticles()
    {
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = _particleSystems[i];

            if (particleSystem == null)
            {
                continue;
            }

            if (_restartIfAlreadyPlaying)
            {
                particleSystem.Stop(
                    withChildren: false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            particleSystem.Play(withChildren: false);
        }
    }

    private void CacheParticleSystems()
    {
        if (_particlesRoot == null)
        {
            _particleSystems = Array.Empty<ParticleSystem>();

            Debug.LogError(
                $"{nameof(PressureButtonParticleFeedback)} requiere el objeto raíz " +
                "que contiene las partículas.",
                this);

            return;
        }

        _particleSystems = _particlesRoot.GetComponentsInChildren<ParticleSystem>(
            includeInactive: true);

        if (_particleSystems.Length == 0)
        {
            Debug.LogWarning(
                $"No se encontraron {nameof(ParticleSystem)} dentro de '{_particlesRoot.name}'.",
                this);
        }
    }

    private void OnValidate()
    {
        if (_stateResolver == null)
        {
            _stateResolver = GetComponent<PressureButtonStateResolver>();
        }
    }
}
