using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class ZoneTile : MonoBehaviour
{
    [Header("Configuración de Zona")]
    [SerializeField] private int targetBuildIndex;
    [SerializeField] private bool isFirstZone;

    [Header("Requisitos de Desbloqueo")]
    [SerializeField] private int requiredBossLevelIndex;
    [SerializeField] private int requiredTotalGems;

    [Header("Referencias de Pilares (Props)")]
    [SerializeField] private Renderer[] gemVisuals; 
    [SerializeField] private GameObject bossScorpionParent;

    [Header("Referencias del Portal (Estructura)")]
    [SerializeField] private GameObject portalStructure;

    [Header("Materiales de Bloqueo")]
    [Tooltip("Material para la estructura del portal cuando está bloqueado.")]
    [SerializeField] private Material lockedPortalMaterial;
    [Tooltip("Material para las gemas y el escorpión cuando están bloqueados (LockedProp).")]
    [SerializeField] private Material lockedPropMaterial;

    [Header("Efectos y Cámara")]
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private Transform revealCamPos;
    [SerializeField] private float revealDuration = 3.0f;
    [SerializeField] private float revealZoomAmount = 2.0f;
    [SerializeField] private AnimationCurve revealZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sistema")]
    [SerializeField] private UIManager uiManager;

    private bool _isUnlocked;
    private bool _playerInside;

    // Diccionarios para guardar los materiales originales de cada grupo
    private Dictionary<Renderer, Material[]> _originalPortalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> _originalPropMaterials = new Dictionary<Renderer, Material[]>();

    void Awake()
    {
        // 1. Cachear materiales originales del Portal
        CacheMaterials(portalStructure, _originalPortalMaterials);

        // 2. Cachear materiales originales de las Gemas
        foreach (var renderer in gemVisuals)
        {
            if (renderer != null && !_originalPropMaterials.ContainsKey(renderer))
                _originalPropMaterials.Add(renderer, renderer.sharedMaterials);
        }

        // 3. Cachear materiales originales del Escorpión
        CacheMaterials(bossScorpionParent, _originalPropMaterials);
    }

    private void CacheMaterials(GameObject parent, Dictionary<Renderer, Material[]> dictionary)
    {
        if (parent == null) return;
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.GetComponent<CanvasRenderer>() != null) continue;
            if (!dictionary.ContainsKey(r))
                dictionary.Add(r, r.sharedMaterials);
        }
    }

    void Start()
    {
        RefreshStatus();

        if (_isUnlocked && !Save.IsZoneRevealSeen(targetBuildIndex))
        {
            TriggerRevealAnimation();
        }
    }

    public void RefreshStatus()
    {
        CheckUnlockConditions();
        UpdateAllVisuals();

        if (portalFx != null)
        {
            if (_isUnlocked && !portalFx.isPlaying) portalFx.Play();
            else if (!_isUnlocked) portalFx.Stop();
        }
    }

    void CheckUnlockConditions()
    {
        if (isFirstZone) { _isUnlocked = true; return; }

        bool bossDefeated = Save.IsLevelCompleted(requiredBossLevelIndex);
        int currentGems = Save.GetGlobalGemCount();
        bool enoughGems = currentGems >= requiredTotalGems;

        _isUnlocked = bossDefeated && enoughGems;
    }

    void UpdateAllVisuals()
    {
        // --- 1. Lógica del Portal ---
        ApplyVisualLogic(_originalPortalMaterials, _isUnlocked, lockedPortalMaterial);

        // --- 2. Lógica del Escorpión ---
        bool bossDefeated = Save.IsLevelCompleted(requiredBossLevelIndex);
        ApplyVisualLogicForScorpion(bossDefeated);

        // --- 3. Lógica de las Gemas (Proporcional) ---
        UpdateGemsVisuals();
    }

    private void ApplyVisualLogic(Dictionary<Renderer, Material[]> cache, bool unlocked, Material lockedMat)
    {
        foreach (var entry in cache)
        {
            Renderer r = entry.Key;
            if (unlocked)
            {
                r.materials = entry.Value; // Restaura originales
            }
            else
            {
                r.materials = CreateLockedArray(entry.Value.Length, lockedMat);
            }
        }
    }

    private void ApplyVisualLogicForScorpion(bool defeated)
    {
        if (bossScorpionParent == null) return;
        Renderer[] renderers = bossScorpionParent.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (_originalPropMaterials.TryGetValue(r, out Material[] originals))
            {
                r.materials = defeated ? originals : CreateLockedArray(originals.Length, lockedPropMaterial);
            }
        }
    }

    private void UpdateGemsVisuals()
    {
        int currentGems = Save.GetGlobalGemCount();
        float progress = Mathf.Clamp01((float)currentGems / requiredTotalGems);
        int gemsToUnlock = Mathf.FloorToInt(progress * gemVisuals.Length);

        for (int i = 0; i < gemVisuals.Length; i++)
        {
            Renderer r = gemVisuals[i];
            if (r == null) continue;

            if (i < gemsToUnlock)
            {
                if (_originalPropMaterials.TryGetValue(r, out Material[] originals))
                    r.materials = originals;
            }
            else
            {
                r.materials = CreateLockedArray(r.sharedMaterials.Length, lockedPropMaterial);
            }
        }
    }

    private Material[] CreateLockedArray(int length, Material lockedMat)
    {
        Material[] mats = new Material[length];
        for (int i = 0; i < length; i++) mats[i] = lockedMat;
        return mats;
    }

    private void Update()
    {
        if (_isUnlocked && _playerInside && Input.GetButtonDown("Space"))
            EnterZone();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = true;
        RefreshStatus();
        if (_isUnlocked) GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.A);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }

    void TriggerRevealAnimation()
    {
        if (FocusManager.Instance != null)
        {
            FocusManager.Instance.RequestRevealFocus(
                targetBuildIndex,
                revealCamPos != null ? revealCamPos : transform,
                transform,
                revealDuration,
                revealZoomAmount,
                revealZoomCurve,
                () => Save.MarkZoneRevealSeen(targetBuildIndex)
            );
        }
    }

    void EnterZone()
    {
        if (uiManager != null)
            uiManager.GetComponent<SceneTransitionManager>().FadeInAndLoadScene(targetBuildIndex);
    }
}