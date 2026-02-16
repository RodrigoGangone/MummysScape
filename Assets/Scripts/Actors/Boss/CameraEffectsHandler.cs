using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Cinemachine;

public class CameraEffectsHandler : MonoBehaviour
{
    [Header("Cinemachine Impulse")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float deathImpulseForce = 1.5f;

    [Header("Post Processing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float blurIntensity = 15f;
    [SerializeField] private float chromaticIntensity = 1f;

    private DepthOfField _dof;
    private ChromaticAberration _chromatic;

    private void Awake()
    {
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out _dof);
            postProcessVolume.profile.TryGet(out _chromatic);
        }
    }

    private void OnEnable()
    {
        GameEventManager.Instance.bossEvents.OnDeath.Register(OnBossDeathEffects);
    }

    private void OnBossDeathEffects()
    {
        // 1. SHAKE: Para aumentar la duración, ajusta el "Time Envelope" 
        // en el componente Impulse Source del Inspector.
        if (impulseSource != null)
            impulseSource.GenerateImpulse(deathImpulseForce);
        
        // 2. BLUR + CHROMATIC (Duración extendida para el impacto)
        StartCoroutine(ImpactVfxCo(0.8f)); 
    }

    private IEnumerator ImpactVfxCo(float duration)
    {
        if (_dof != null) _dof.active = true;
        if (_chromatic != null) _chromatic.active = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curve = Mathf.Sin(t * Mathf.PI); // Curva de campana
            
            if (_dof != null) _dof.focalLength.value = curve * blurIntensity;
            if (_chromatic != null) _chromatic.intensity.value = curve * chromaticIntensity;

            yield return null;
        }

        if (_dof != null) _dof.active = false;
        if (_chromatic != null) _chromatic.active = false;
    }
}