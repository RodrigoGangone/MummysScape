using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> 
/// Gestor de Nivel: Controla el estado de desbloqueo de un nivel individual, gestionando la visualización 
/// de gemas obtenidas y disparando secuencias de "revelación" de cámara. 
/// </summary>
public class LevelTile : MonoBehaviour
{
    [Header("Configuración Nivel")]
    [Tooltip("El índice de la escena para verificar progreso y gemas.")]
    [SerializeField]
    private int buildIndex;

    [SerializeField] private bool isFirstLevel;
    [SerializeField] private bool isBossLevel;

    [Header("Referencias Visuales")] [SerializeField]
    private ParticleSystem portalFx;

    [SerializeField] private GameObject[] gemIcons = new GameObject[3];
    [SerializeField] private Material lockedMaterial;

    [Header("Focus Reveal (Al desbloquearse)")] [SerializeField]
    private float revealDuration = 3.0f;

    [SerializeField] private float revealZoomAmount = 2.0f;
    [SerializeField] private AnimationCurve revealZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Referencias de Cámara")] [SerializeField]
    private Transform entryFocusPos;

    [SerializeField] private Transform entryLookAt;

    bool _isUnlocked;

    void Start()
    {
        bool isValidIndex = IsBuildIndexValid(buildIndex);

        if (isFirstLevel) _isUnlocked = true;
        else _isUnlocked = Save.IsLevelCompleted(buildIndex - 1);

        if (!_isUnlocked || !isValidIndex)
        {
            ApplyLockedMaterial();
            SetAllGems(false);

            var portal = GetComponentInChildren<Portal>(true);
            if (portal != null)
            {
                portal.enabled = false;

                // Si el GameObject del portal tiene SphereCollider lo desactivamos
                SphereCollider sphere = portal.GetComponent<SphereCollider>();
                if (sphere != null)
                    sphere.enabled = false;
            }
            
            return;
        }

        if (!Save.IsLevelRevealSeen(buildIndex))
        {
            if (FocusManager.Instance != null)
            {
                FocusManager.Instance.RequestRevealFocus(
                    buildIndex,
                    entryFocusPos != null ? entryFocusPos : transform,
                    entryLookAt,
                    revealDuration,
                    revealZoomAmount,
                    revealZoomCurve,
                    () => Save.MarkLevelRevealSeen(buildIndex)
                );
            }
        }
        
        if (!isBossLevel) RefreshGems();
        else SetAllGems(false);
    }

    private void ApplyLockedMaterial()
    {
        if (lockedMaterial == null) return;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = lockedMaterial;
            r.materials = mats;
        }
    }

    private void RefreshGems()
    {
        for (int i = 0; i < gemIcons.Length; i++)
        {
            if (gemIcons[i] == null) continue;
            bool picked = Save.WasGemPickedInLevel(i + 1, buildIndex);
            gemIcons[i].SetActive(picked);
        }
    }

    private void SetAllGems(bool on)
    {
        foreach (var g in gemIcons)
            if (g)
                g.SetActive(on);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather") && _isUnlocked)
            portalFx.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather") && _isUnlocked)
            portalFx.Stop();
    }
    
    private bool IsBuildIndexValid(int index)
    {
        //path vacío si el índice no existe en BuildSettings
        return !string.IsNullOrEmpty(SceneUtility.GetScenePathByBuildIndex(index));
    }
}