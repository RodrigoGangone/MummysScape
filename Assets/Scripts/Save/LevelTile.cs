using UnityEngine;

public class LevelTile : MonoBehaviour
{
    [Header("Configuración Nivel")]
    [Tooltip("El índice de la escena para verificar progreso y gemas.")]
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel;
    [SerializeField] private bool isBossLevel;

    [Header("Referencias Visuales")]
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];
    [SerializeField] private Material lockedMaterial;

    [Header("Focus Reveal (Al desbloquearse)")]
    [SerializeField] private float revealDuration = 3.0f;
    [SerializeField] private float revealZoomAmount = 2.0f;
    [SerializeField] private AnimationCurve revealZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Referencias de Cámara")]
    [SerializeField] private Transform entryFocusPos;
    [SerializeField] private Transform entryLookAt;

    bool _isUnlocked;

    void Start()
    {
        // 1. Lógica de Desbloqueo
        if (isFirstLevel) _isUnlocked = true;
        else
        {
            // Se desbloquea si el nivel anterior está completo
            _isUnlocked = Save.IsLevelCompleted(buildIndex - 1);
        }

        // 2. Estado Visual Bloqueado
        if (!_isUnlocked)
        {
            ApplyLockedMaterial();
            SetAllGems(false);
            if (portalFx != null) portalFx.Stop();
            
            // Si está bloqueado, podemos desactivar el Portal (Sarcófago) hijo para que no interactúe
            var portal = GetComponentInChildren<Portal>();
            if (portal != null) portal.gameObject.SetActive(false);
            
            return;
        }

        // 3. Lógica de REVEAL (Cámara enfocando nivel nuevo)
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

        // 4. Mostrar Gemas
        if (!isBossLevel) RefreshGems();
        else SetAllGems(false);

        if (portalFx != null) portalFx.Play();
    }

    private void ApplyLockedMaterial()
    {
        if (lockedMaterial == null) return;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = lockedMaterial;
            r.materials = mats;
        }
    }

    private void RefreshGems()
    {
        for (int i = 0; i < gemIcons.Length; i++) {
            if (gemIcons[i] == null) continue;
            bool picked = Save.WasGemPickedInLevel(i + 1, buildIndex);
            gemIcons[i].SetActive(picked);
        }
    }

    private void SetAllGems(bool on) { foreach (var g in gemIcons) if (g) g.SetActive(on); }
}