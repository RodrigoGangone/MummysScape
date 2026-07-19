using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static Tags; 

public class LevelTile : MonoBehaviour
{
    [Header("Configuración Nivel")]
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel;
    [SerializeField] private Portal portal;

    [Header("Referencias Visuales")]
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private Material lockedMaterial;

    [Header("Gemas")]
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];
    [SerializeField] private float gemPulseScale = 1.5f;
    [SerializeField] private float gemPulseDuration = 0.5f;

    [Header("Timeline & Reveal")]
    [SerializeField] private PlayableDirector director;

    [Header("Animación de Material")]
    [SerializeField] private float glowDuration = 2.0f;
    [SerializeField] private float cutOffDuration = 1.5f;

    [Header("Referencias Generales")]
    [SerializeField] private Transform playerPos;

    public int BuildIndex => buildIndex;
    public Transform PlayerPos => playerPos;

    private bool _isUnlocked;
    private Action _onRevealCompleteCallback;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    
    private static readonly int GlowStrengthProperty = Shader.PropertyToID("_Glow_Strength");
    private static readonly int CutOffHeightProperty = Shader.PropertyToID("_Cut_Off_Height");

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }
    
    public bool EvaluateStateAndCheckReveal()
    {
        if (!IsBuildIndexValid(buildIndex))
        {
            Debug.LogWarning($"El Build Index {buildIndex} no es válido en el objeto {gameObject.name}.");
            return false;
        }

        _isUnlocked = isFirstLevel || Save.IsLevelCompleted(buildIndex - 1);
        bool hasSeenReveal = Save.IsLevelRevealSeen(buildIndex);
        
        bool needsReveal = _isUnlocked && !hasSeenReveal;

        if (!_isUnlocked || needsReveal)
        {
            ApplyLockedMaterial();
            SetAllGems(false);
            if (portal != null) portal.enabled = false;
        }
        else
        {
            RefreshGemsInstant();
            if (portal != null) portal.enabled = true;
        }

        // Devolvemos la respuesta al Manager para que él decida si lo encola o no
        return needsReveal;
    }

    public void PlayRevealSequence(Action onComplete)
    {
        _onRevealCompleteCallback = onComplete;

        if (director != null)
        {
            director.stopped += OnTimelineStopped;
            director.Play();
        }
        else
        {
            Debug.Log("Fail - Reveal");
            CompleteReveal();
        }
    }

    // =========================================================
    // MÉTODOS PÚBLICOS DE ANIMACIÓN DE MATERIALES
    // =========================================================

    public void AnimateGlowBlink()
    {
        StartCoroutine(GlowBlinkRoutine());

        IEnumerator GlowBlinkRoutine()
        {
            float timer = 0f;
            float halfGlow = glowDuration / 2f;

            while (timer < halfGlow)
            {
                timer += Time.deltaTime;
                ApplyPropertyValue(GlowStrengthProperty, Mathf.Lerp(0f, 1f, timer / halfGlow));
                yield return null;
            }

            timer = 0f;
            while (timer < halfGlow)
            {
                timer += Time.deltaTime;
                ApplyPropertyValue(GlowStrengthProperty, Mathf.Lerp(1f, 0f, timer / halfGlow));
                yield return null;
            }

            ApplyPropertyValue(GlowStrengthProperty, 0f);
        }
    }

    public void AnimateCutOffReveal()
    {
        StartCoroutine(CutOffRoutine());

        IEnumerator CutOffRoutine()
        {
            float timer = 0f;
            float startCutOff = lockedMaterial != null ? lockedMaterial.GetFloat(CutOffHeightProperty) : 1f;

            while (timer < cutOffDuration)
            {
                timer += Time.deltaTime;
                ApplyPropertyValue(CutOffHeightProperty, Mathf.Lerp(startCutOff, 0f, timer / cutOffDuration));
                yield return null;
            }

            ApplyPropertyValue(CutOffHeightProperty, 0f);
        }
    }

    // =========================================================
    // MÉTODOS DE GEMAS (TIMELINE & ESTÁTICOS)
    // =========================================================
    
    private void RefreshGemsInstant()
    {
        for (int i = 0; i < gemIcons.Length; i++)
        {
            if (gemIcons[i] != null)
            {
                gemIcons[i].SetActive(Save.WasGemPickedInLevel(i + 1, buildIndex));
            }
        }
    }

    private void SetAllGems(bool state)
    {
        foreach (var gem in gemIcons)
        {
            if (gem != null) gem.SetActive(state);
        }
    }

    // =========================================================
    // MÉTODOS PRIVADOS Y DE SOPORTE
    // =========================================================

    private void ApplyPropertyValue(int propertyId, float value)
    {
        if (_renderers == null || _renderers.Length == 0) return;

        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(propertyId, value);
            r.SetPropertyBlock(_propBlock);
        }
    }

    private void OnTimelineStopped(PlayableDirector dir)
    {
        if (director != null)
        {
            director.stopped -= OnTimelineStopped;
        }
        CompleteReveal();
    }

    private void CompleteReveal()
    {
        if (portal != null) portal.enabled = true;
        
        Save.MarkLevelRevealSeen(buildIndex);
        _onRevealCompleteCallback?.Invoke();
    }

    private void ApplyLockedMaterial()
    {
        if (lockedMaterial == null) return;

        foreach (var r in _renderers)
        {
            Material[] currentMats = r.sharedMaterials;
            Material[] newMats = new Material[currentMats.Length + 1];

            Array.Copy(currentMats, newMats, currentMats.Length);
            newMats[currentMats.Length] = lockedMaterial;

            r.materials = newMats;
        }
    }

    private bool IsBuildIndexValid(int index)
    {
        return !string.IsNullOrEmpty(SceneUtility.GetScenePathByBuildIndex(index));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isUnlocked && other.CompareTag(PLAYER_TAG))
        {
            if (portalFx != null) portalFx.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isUnlocked && other.CompareTag(PLAYER_TAG))
        {
            if (portalFx != null) portalFx.Stop();
        }
    }
}