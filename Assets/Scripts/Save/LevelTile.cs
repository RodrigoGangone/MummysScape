using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static Tags;

public class LevelTile : MonoBehaviour
{
    [Header("Configuración Nivel")]
    [Tooltip("El índice de la escena para verificar progreso.")]
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel;

    [Header("Referencias Visuales")]
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private Material lockedMaterial;

    [Header("Gemas")]
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];
    [Tooltip("Multiplicador de tamaño máximo durante el latido")]
    [SerializeField] private float gemPulseScale = 1.5f;
    [Tooltip("Duración total del latido (crecer y achicarse)")]
    [SerializeField] private float gemPulseDuration = 0.5f;

    [Header("Timeline & Reveal")]
    [SerializeField] private PlayableDirector director;

    [Header("Animación de Material")]
    [SerializeField] private float glowDuration = 2.0f;
    [SerializeField] private float cutOffDuration = 1.5f;

    [Header("Referencias Generales")]
    [SerializeField] private Transform playerPos;

    // Propiedades públicas
    public int BuildIndex => buildIndex;
    public Transform PlayerPos => playerPos;

    // Variables de estado interno
    private bool _isUnlocked;
    private Action _onRevealCompleteCallback;
    private int _currentGemRevealIndex = 0; // Lleva la cuenta de qué gema toca animar en la Timeline

    // Caché estricto para evitar alocaciones en tiempo de ejecución (GC Alloc)
    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    
    private static readonly int GlowStrengthProperty = Shader.PropertyToID("_Glow_Strength");
    private static readonly int CutOffHeightProperty = Shader.PropertyToID("_Cut_Off_Height");

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        if (director != null)
        {
            director.playOnAwake = false; 
        }
    }

    // ELIMINAMOS EL START. Ahora el Manager llama a este método.
    
    /// <summary>
    /// Configura el estado visual del Tile y devuelve TRUE si necesita ser encolado en el RevealManager.
    /// </summary>
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
        }
        else
        {
            RefreshGemsInstant();
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

            // Fase de subida
            while (timer < halfGlow)
            {
                timer += Time.deltaTime;
                ApplyPropertyValue(GlowStrengthProperty, Mathf.Lerp(0f, 1f, timer / halfGlow));
                yield return null;
            }

            // Fase de bajada
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
            
            // Leemos el valor exacto desde el material base
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

    /// <summary>
    /// Llamado desde un Signal Receiver de la Timeline. 
    /// Procesa automáticamente la siguiente gema en el array.
    /// </summary>
    public void AnimateNextGemReveal()
    {
        // Si ya nos pasamos de la cantidad de gemas, salimos
        if (_currentGemRevealIndex >= gemIcons.Length) return;

        int gemIndex = _currentGemRevealIndex;
        _currentGemRevealIndex++; // Preparamos el índice para el próximo llamado de la Timeline

        if (gemIcons[gemIndex] == null) return;

        // Verificamos si realmente se agarró en el nivel. 
        if (Save.WasGemPickedInLevel(gemIndex + 1, buildIndex))
        {
            StartCoroutine(GemPulseRoutine(gemIcons[gemIndex].transform));
        }

        // Función local: Encapsula el Lerp de escala (Latido)
        IEnumerator GemPulseRoutine(Transform gemTransform)
        {
            gemTransform.gameObject.SetActive(true);
            
            Vector3 originalScale = gemTransform.localScale;
            Vector3 targetScale = originalScale * gemPulseScale;
            
            float halfDuration = gemPulseDuration / 2f;
            float timer = 0f;

            // Fase 1: Latido Arriba (Crece)
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                gemTransform.localScale = Vector3.Lerp(originalScale, targetScale, timer / halfDuration);
                yield return null;
            }

            // Fase 2: Latido Abajo (Vuelve a su tamaño)
            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                gemTransform.localScale = Vector3.Lerp(targetScale, originalScale, timer / halfDuration);
                yield return null;
            }

            // Aseguramos la escala exacta al terminar por cuestiones de precisión
            gemTransform.localScale = originalScale;
        }
    }

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