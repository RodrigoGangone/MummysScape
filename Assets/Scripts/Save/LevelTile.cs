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

    [Header("Focus Entry (Animación al Entrar)")]
    [Tooltip("Posición de la cámara cuando el jugador presiona X para entrar.")]
    [SerializeField] private Transform entryFocusPos;
    [Tooltip("Hacia dónde mira la cámara al entrar (ej. al portal).")]
    [SerializeField] private Transform entryLookAt;
    [SerializeField] private float entryDuration = 2.0f;

    [Header("Sistema")]
    [SerializeField] private UIManager uiManager;

    // ==========================================
    // SECCIÓN DEBUG (Solo en Editor)
    // ==========================================
#if UNITY_EDITOR
    [Header("--- DEBUG TOOLS ---")]
    [Tooltip("Si es true, borra la key 'Seen' al iniciar, forzando la animación de cámara.")]
    [SerializeField] private bool debugResetSeenOnPlay;
    
    [Tooltip("Si es true, el nivel estará abierto ignorando el nivel anterior.")]
    [SerializeField] private bool debugForceUnlock;

    [ContextMenu("Borrar Datos de ESTE Nivel (Lock & Unseen)")]
    void ClearThisLevelData()
    {
        // Borrar el "Visto"
        PlayerPrefs.DeleteKey($"seen.level_reveal.{buildIndex}");
        // Opcional: Borrar el nivel anterior para bloquear este
        // PlayerPrefs.DeleteKey($"level.completed.index.{buildIndex - 1}");
        Debug.Log($"[LevelTile Debug] Reset 'Seen' para nivel {buildIndex}");
    }
#endif
    // ==========================================

    // Cache
    bool _playerInside;
    bool _isUnlocked;

    void Start()
    {
        // --- 0. DEBUG: Resetear estado 'Seen' ---
#if UNITY_EDITOR
        if (debugResetSeenOnPlay)
        {
            PlayerPrefs.DeleteKey($"seen.level_reveal.{buildIndex}");
        }
#endif

        // --- 1. Validaciones ---
        if (buildIndex == 0 && !isFirstLevel)
            Debug.LogWarning($"[LevelTile] '{name}' tiene BuildIndex 0 pero no es FirstLevel.", gameObject);

        // --- 2. Lógica Unlocked ---
        if (isFirstLevel)
        {
            _isUnlocked = true;
        }
        else
        {
#if UNITY_EDITOR
            _isUnlocked = debugForceUnlock || Save.IsLevelCompleted(buildIndex - 1);
#else
            _isUnlocked = Save.IsLevelCompleted(buildIndex - 1);
#endif
        }

        // --- 3. Estado Bloqueado ---
        if (!_isUnlocked)
        {
            ApplyLockedMaterial();
            SetAllGems(false);
            if (portalFx != null) portalFx.Stop();
            return;
        }

        // --- 4. Estado Desbloqueado ---
        // Respetando tu lógica: Apagamos FX al inicio, solo se prenden al entrar al trigger
        if (portalFx != null) portalFx.Stop();

        // --- 5. REVEAL (Seen) ---
        if (!Save.IsLevelRevealSeen(buildIndex))
        {
            if (FocusManager.Instance != null)
            {
                // CAMBIO IMPORTANTE:
                // Pasamos 'buildIndex' como primer parámetro. 
                // Esto le dice al FocusManager: "Mi turno es el número X".
                FocusManager.Instance.RequestRevealFocus(
                    buildIndex, // <--- LA CLAVE DEL ORDENAMIENTO
                    entryFocusPos != null ? entryFocusPos : transform,
                    entryLookAt, 
                    entryDuration,
                    () => Save.MarkLevelRevealSeen(buildIndex)
                );
            }
        }

        // --- 6. Gemas ---
        if (!isBossLevel) RefreshGems();
        else SetAllGems(false);

        // Reset Prompt UI
        GameEventManager.Instance?.levelEvents.OnTutorialPrompt.Raise(false, buttonType.A);
    }

    private void Update()
    {
        if (_playerInside && _isUnlocked)
        {
            if (Input.GetButtonDown("Space"))
                EnterLevel();
        }
    }

    void EnterLevel()
    {
        // 1. Disparar el Focus de Entrada
        if (FocusManager.Instance != null)
        {
            Transform camPos = entryFocusPos != null ? entryFocusPos : transform;
            Transform lookAt = entryLookAt != null ? entryLookAt : transform;

            FocusManager.Instance.RequestObjectFocus(camPos, lookAt, entryDuration);
        }

        // 2. Cargar Escena
        if (uiManager != null)
        {
            var transition = uiManager.GetComponent<SceneTransitionManager>();
            if (transition != null)
                transition.FadeInAndLoadScene(buildIndex);
        }
    }

    // --- Helpers Visuales ---

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
        foreach (var g in gemIcons) if (g) g.SetActive(on);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isUnlocked || !other.CompareTag("PlayerFather")) return;
        
        _playerInside = true;
        
        // FX se prende al entrar (Tu lógica original)
        if (portalFx != null) portalFx.Play();
        
        if (!isBossLevel) RefreshGems();
        GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(true, buttonType.A);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isUnlocked || !other.CompareTag("PlayerFather")) return;
        
        _playerInside = false;
        
        // FX se apaga al salir (Tu lógica original)
        if (portalFx != null) portalFx.Stop();
        
        GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(false, buttonType.A);
    }
}