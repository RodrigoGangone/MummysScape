using System.Collections;
using UnityEngine;

public class SelectorNodeVisuals : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] renderers;
    [Header("Locked Feedback")]
    [SerializeField] private ParticleSystem lockedFx;
    [SerializeField] private Material lockedMaterial;

    [Header("State Indicators")]
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private GameObject completedIndicator;
    [SerializeField] private ParticleSystem selectedFx;

    [Header("Legacy Reveal Shader")]
    [SerializeField] private float glowDuration = 2f;
    [SerializeField] private float cutOffDuration = 1.5f;

    private Material[][] _originalMaterials;
    private MaterialPropertyBlock _propertyBlock;

    private static readonly int GlowStrengthProperty =
        Shader.PropertyToID("_Glow_Strength");

    private static readonly int CutOffHeightProperty =
        Shader.PropertyToID("_Cut_Off_Height");

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        _originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            _originalMaterials[i] =
                renderers[i] != null
                    ? renderers[i].sharedMaterials
                    : null;
        }

        _propertyBlock = new MaterialPropertyBlock();
    }

    public void ApplyLevelState(LevelTileState state)
    {
        bool visuallyLocked =
            state.Availability == LevelAvailability.Locked ||
            state.RevealPending;

        SetLocked(visuallyLocked);

        if (completedIndicator != null)
        {
            completedIndicator.SetActive(
                state.Availability == LevelAvailability.Completed &&
                !state.RevealPending
            );
        }
    }

    public void ApplyZoneState(ZoneTileState state)
    {
        SetLocked(!state.IsUnlocked);

        if (completedIndicator != null)
            completedIndicator.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        if (selectedFx == null)
            return;

        if (selected)
        {
            if (!selectedFx.isPlaying)
                selectedFx.Play();
        }
        else
        {
            if (selectedFx.isPlaying)
            {
                selectedFx.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }
    }

    public void SetLocked(bool locked)
    {
        ApplyLockedMaterials(locked);
        ApplyLockedFx(locked);
    }
    
    private void ApplyLockedMaterials(bool locked)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (!locked || lockedMaterial == null)
            {
                if (_originalMaterials != null &&
                    i < _originalMaterials.Length &&
                    _originalMaterials[i] != null)
                {
                    renderer.sharedMaterials =
                        _originalMaterials[i];
                }

                continue;
            }

            Material[] source =
                _originalMaterials != null &&
                i < _originalMaterials.Length
                    ? _originalMaterials[i]
                    : renderer.sharedMaterials;

            int materialCount =
                source != null
                    ? source.Length
                    : 1;

            Material[] lockedMaterials =
                new Material[materialCount];

            for (int j = 0; j < materialCount; j++)
                lockedMaterials[j] = lockedMaterial;

            renderer.sharedMaterials =
                lockedMaterials;
        }
    }
    
    private void ApplyLockedFx(bool locked)
    {
        if (lockedFx == null)
            return;

        if (locked)
        {
            if (!lockedFx.isPlaying)
                lockedFx.Play(true);
        }
        else
        {
            if (lockedFx.isPlaying)
            {
                lockedFx.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }
    }
    
    public void AnimateGlowBlink()
    {
        StartCoroutine(GlowBlinkRoutine());
    }

    public void AnimateCutOffReveal()
    {
        StartCoroutine(CutOffRoutine());
    }

    private IEnumerator GlowBlinkRoutine()
    {
        float halfGlow =
            Mathf.Max(0.01f, glowDuration * 0.5f);

        float elapsed = 0f;

        while (elapsed < halfGlow)
        {
            elapsed += Time.deltaTime;

            ApplyPropertyValue(
                GlowStrengthProperty,
                Mathf.Lerp(
                    0f,
                    1f,
                    elapsed / halfGlow
                )
            );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfGlow)
        {
            elapsed += Time.deltaTime;

            ApplyPropertyValue(
                GlowStrengthProperty,
                Mathf.Lerp(
                    1f,
                    0f,
                    elapsed / halfGlow
                )
            );

            yield return null;
        }

        ApplyPropertyValue(
            GlowStrengthProperty,
            0f
        );
    }

    private IEnumerator CutOffRoutine()
    {
        float startCutOff =
            lockedMaterial != null &&
            lockedMaterial.HasProperty(CutOffHeightProperty)
                ? lockedMaterial.GetFloat(CutOffHeightProperty)
                : 1f;

        float duration =
            Mathf.Max(0.01f, cutOffDuration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            ApplyPropertyValue(
                CutOffHeightProperty,
                Mathf.Lerp(
                    startCutOff,
                    0f,
                    elapsed / duration
                )
            );

            yield return null;
        }

        ApplyPropertyValue(
            CutOffHeightProperty,
            0f
        );
    }

    private void ApplyPropertyValue(
        int propertyId,
        float value)
    {
        if (renderers == null ||
            _propertyBlock == null)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(
                _propertyBlock
            );

            _propertyBlock.SetFloat(
                propertyId,
                value
            );

            renderer.SetPropertyBlock(
                _propertyBlock
            );
        }
    }
}