using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelTile : MonoBehaviour
{
    [Header("Configuración Nivel")]
    [Tooltip("El índice de la escena en Build Settings.")]
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel;
    [SerializeField] private bool isBossLevel;

    [Header("Referencias Visuales")]
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];
    [SerializeField] private Material lockedMaterial;

    // --- SECCIÓN REVEAL (Cuando se desbloquea automáticamente) ---
    [Header("Focus Reveal (Al desbloquearse)")]
    [Tooltip("Cuánto tiempo se queda la cámara mirando el nivel desbloqueado.")]
    [SerializeField] private float revealDuration = 3.0f; // <--- NUEVA VARIABLE
    [Tooltip("Zoom suave para mostrar que el nivel está disponible.")]
    [SerializeField] private float revealZoomAmount = 2.0f;
    [SerializeField] private AnimationCurve revealZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // --- SECCIÓN ENTRY (Cuando el jugador entra) ---
    [Header("Focus Entry (Animación al Entrar)")]
    [Tooltip("Posición de la cámara para la entrada.")]
    [SerializeField] private Transform entryFocusPos;
    [Tooltip("Hacia dónde mira la cámara.")]
    [SerializeField] private Transform entryLookAt;
    [Tooltip("Cuánto tarda la animación al entrar (usualmente más rápido).")]
    [SerializeField] private float entryDuration = 1.5f; // <--- SEPARADA
    [Tooltip("Zoom agresivo hacia el portal.")]
    [SerializeField] private float entryZoomAmount = 4.0f;
    [SerializeField] private AnimationCurve entryZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sistema")]
    [SerializeField] private UIManager uiManager;

    // ==========================================
    // SECCIÓN DEBUG (Solo en Editor)
    // ==========================================
#if UNITY_EDITOR
    [Header("--- DEBUG TOOLS ---")]
    [SerializeField] private bool debugResetSeenOnPlay;
    [SerializeField] private bool debugForceUnlock;

    [ContextMenu("Borrar Datos de ESTE Nivel")]
    void ClearThisLevelData()
    {
        PlayerPrefs.DeleteKey($"seen.level_reveal.{buildIndex}");
        Debug.Log($"[LevelTile Debug] Reset 'Seen' para nivel {buildIndex}");
    }
#endif
    // ==========================================

    bool _playerInside;
    bool _isUnlocked;

    void Start()
    {
#if UNITY_EDITOR
        if (debugResetSeenOnPlay) PlayerPrefs.DeleteKey($"seen.level_reveal.{buildIndex}");
#endif

        if (buildIndex == 0 && !isFirstLevel) Debug.LogWarning($"[LevelTile] Indice 0 sin FirstLevel.", gameObject);

        if (isFirstLevel) _isUnlocked = true;
        else
        {
#if UNITY_EDITOR
            _isUnlocked = debugForceUnlock || Save.IsLevelCompleted(buildIndex - 1);
#else
            _isUnlocked = Save.IsLevelCompleted(buildIndex - 1);
#endif
        }

        if (!_isUnlocked)
        {
            ApplyLockedMaterial();
            SetAllGems(false);
            if (portalFx != null) portalFx.Stop();
            return;
        }

        if (portalFx != null) portalFx.Stop();

        // --- 5. REVEAL (Seen) ---
        if (!Save.IsLevelRevealSeen(buildIndex))
        {
            if (FocusManager.Instance != null)
            {
                FocusManager.Instance.RequestRevealFocus(
                    buildIndex, 
                    entryFocusPos != null ? entryFocusPos : transform,
                    entryLookAt, 
                    revealDuration,   // <--- CORREGIDO: Usamos la duración de reveal
                    revealZoomAmount, 
                    revealZoomCurve,  
                    () => Save.MarkLevelRevealSeen(buildIndex)
                );
            }
        }

        if (!isBossLevel) RefreshGems();
        else SetAllGems(false);

        GameEventManager.Instance?.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }

    private void Update()
    {
        if (_playerInside && _isUnlocked)
        {
            if (Input.GetButtonDown("Space")) EnterLevel();
        }
    }

    void EnterLevel()
    {
        if (FocusManager.Instance != null)
        {
            Transform camPos = entryFocusPos != null ? entryFocusPos : transform;
            Transform lookAt = entryLookAt != null ? entryLookAt : transform;

            FocusManager.Instance.RequestObjectFocus(
                camPos, 
                lookAt, 
                entryDuration,    // <--- Usamos la duración de entrada
                entryZoomAmount, 
                entryZoomCurve   
            );
        }

        if (uiManager != null)
        {
            var transition = uiManager.GetComponent<SceneTransitionManager>();
            if (transition != null) transition.FadeInAndLoadScene(buildIndex);
        }
    }

    // --- Helpers Visuales ---
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

    private void OnTriggerEnter(Collider other)
    {
        if (!_isUnlocked || !other.CompareTag("PlayerFather")) return;
        _playerInside = true;
        if (portalFx != null) portalFx.Play();
        if (!isBossLevel) RefreshGems();
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.A);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isUnlocked || !other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        if (portalFx != null) portalFx.Stop();
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }
}