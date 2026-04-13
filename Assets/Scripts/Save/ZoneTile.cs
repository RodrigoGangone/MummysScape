using UnityEngine;
using System.Collections.Generic;
using static Tags;

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

    [Header("UI Contextual")]
    [SerializeField] private string enterZonePromptText = "Entrar";

    private bool _isUnlocked;
    private bool _playerInside;

    private readonly Dictionary<Renderer, Material[]> _originalPortalMaterials = new();
    private readonly Dictionary<Renderer, Material[]> _originalPropMaterials = new();

    private void Awake()
    {
        CacheMaterials(portalStructure, _originalPortalMaterials);

        foreach (var renderer in gemVisuals)
        {
            if (renderer != null && !_originalPropMaterials.ContainsKey(renderer))
                _originalPropMaterials.Add(renderer, renderer.sharedMaterials);
        }

        CacheMaterials(bossScorpionParent, _originalPropMaterials);
    }

    private void Start()
    {
        RefreshStatus();

        if (_isUnlocked && !Save.IsZoneRevealSeen(targetBuildIndex))
            TriggerRevealAnimation();
    }

    public void RefreshStatus()
    {
        CheckUnlockConditions();
        UpdateAllVisuals();

        if (portalFx != null)
        {
            if (_isUnlocked && !portalFx.isPlaying)
                portalFx.Play();
            else if (!_isUnlocked)
                portalFx.Stop();
        }
    }

    private void CheckUnlockConditions()
    {
        if (isFirstZone)
        {
            _isUnlocked = true;
            return;
        }

        bool bossDefeated = Save.IsLevelCompleted(requiredBossLevelIndex);
        int currentGems = Save.GetGlobalGemCount();
        bool enoughGems = currentGems >= requiredTotalGems;

        _isUnlocked = bossDefeated && enoughGems;
    }

    private void UpdateAllVisuals()
    {
        ApplyVisualLogic(_originalPortalMaterials, _isUnlocked, lockedPortalMaterial);

        bool bossDefeated = Save.IsLevelCompleted(requiredBossLevelIndex);
        ApplyVisualLogicForScorpion(bossDefeated);

        UpdateGemsVisuals();
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

    private void ApplyVisualLogic(Dictionary<Renderer, Material[]> cache, bool unlocked, Material lockedMat)
    {
        foreach (var entry in cache)
        {
            Renderer r = entry.Key;

            if (unlocked)
                r.materials = entry.Value;
            else
                r.materials = CreateLockedArray(entry.Value.Length, lockedMat);
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
                r.materials = defeated
                    ? originals
                    : CreateLockedArray(originals.Length, lockedPropMaterial);
            }
        }
    }

    private void UpdateGemsVisuals()
    {
        if (gemVisuals == null || gemVisuals.Length == 0) return;

        int currentGems = Save.GetGlobalGemCount();

        if (requiredTotalGems <= 0)
        {
            for (int i = 0; i < gemVisuals.Length; i++)
            {
                Renderer r = gemVisuals[i];
                if (r == null) continue;

                if (_originalPropMaterials.TryGetValue(r, out Material[] originals))
                    r.materials = originals;
            }

            return;
        }

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

        for (int i = 0; i < length; i++)
            mats[i] = lockedMat;

        return mats;
    }

    private void Update()
    {
        if (_isUnlocked && _playerInside && Input.GetButtonDown("Accept"))
            EnterZone();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;

        _playerInside = true;
        RefreshStatus();

        if (_isUnlocked)
        {
            GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
                ContextUIFactory.Prompt(ContextMessageType.Enter, ButtonType.Y)
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;

        _playerInside = false;

        GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
            ContextUIFactory.Hidden()
        );
    }

    private void TriggerRevealAnimation()
    {
        if (FocusManager.Instance == null) return;

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

    private void EnterZone()
    {
        if (uiManager == null) return;

        GameEventManager.Instance.levelEvents.OnContextUIChanged.Raise(
            ContextUIFactory.Hidden()
        );

        uiManager.GetComponent<SceneTransitionManager>().FadeInAndLoadScene(targetBuildIndex);
    }
}