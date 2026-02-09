using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZoneTile : MonoBehaviour
{
    [Header("Configuración de Zona")]
    [Tooltip("A qué escena (Build Index) viaja este portal.")]
    [SerializeField] private int targetBuildIndex;
    [SerializeField] private bool isFirstZone; 

    [Header("Requisitos")]
    [Tooltip("El nivel (índice) del Boss anterior que debe estar completado.")]
    [SerializeField] private int requiredBossLevelIndex;
    [Tooltip("Cantidad TOTAL de gemas requeridas (Suma global de todo el juego).")]
    [SerializeField] private int requiredTotalGems;

    [Header("Referencias Visuales")]
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private Material lockedMaterial;

    // --- SECCIÓN REVEAL (Cuando se desbloquea automáticamente) ---
    [Header("Focus Reveal (Animación de Desbloqueo)")]
    [SerializeField] private Transform revealCamPos;
    [SerializeField] private float revealDuration = 3.0f;
    [Tooltip("Zoom suave para mostrar panorámicamente que se abrió el camino.")]
    [SerializeField] private float revealZoomAmount = 2.0f; 
    [SerializeField] private AnimationCurve revealZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // --- SECCIÓN ENTRY (Cuando el jugador entra) ---
    [Header("Focus Entry (Animación al Entrar)")]
    [SerializeField] private Transform entryFocusPos;
    [SerializeField] private Transform entryLookAt;
    [SerializeField] private float entryDuration = 2.0f;
    [Tooltip("Zoom más intenso hacia el portal al entrar.")]
    [SerializeField] private float entryZoomAmount = 4.0f;
    [SerializeField] private AnimationCurve entryZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sistema")]
    [SerializeField] private UIManager uiManager;

    // ==========================================
    // SECCIÓN DEBUG (Solo en Editor)
    // ==========================================
#if UNITY_EDITOR
    [Header("--- DEBUG TOOLS ---")]
    [Tooltip("Si es true, borra la key 'Seen' al iniciar, forzando la animación de cámara.")]
    [SerializeField] private bool debugResetSeenOnPlay;

    [Tooltip("Si es true, la zona estará abierta ignorando Boss y Gemas.")]
    [SerializeField] private bool debugForceUnlock;

    [ContextMenu("Borrar Datos de ESTA Zona")]
    void ClearZoneData()
    {
        PlayerPrefs.DeleteKey($"seen.zone_reveal.{targetBuildIndex}");
        Debug.Log($"[ZoneTile Debug] Reset 'Seen' para zona {targetBuildIndex}");
    }
#endif
    // ==========================================

    bool _isUnlocked;
    bool _playerInside;

    void Start()
    {
        // --- 0. DEBUG: Resetear estado 'Seen' ---
#if UNITY_EDITOR
        if (debugResetSeenOnPlay)
        {
            PlayerPrefs.DeleteKey($"seen.zone_reveal.{targetBuildIndex}");
        }
#endif

        // --- 1. Chequeo de condiciones ---
        CheckUnlockConditions();

        // --- 2. Estado Bloqueado ---
        if (!_isUnlocked)
        {
            ApplyLockedMaterial();
            if (portalFx != null) portalFx.Stop();
            return;
        }

        // --- 3. Estado Desbloqueado ---
        if (portalFx != null) portalFx.Play();

        // --- 4. Lógica de Revelación (Cola de Foco) ---
        if (!Save.IsZoneRevealSeen(targetBuildIndex))
        {
            if (FocusManager.Instance != null)
            {
                // Usamos los parámetros de REVEAL
                FocusManager.Instance.RequestRevealFocus(
                    targetBuildIndex, // Prioridad
                    revealCamPos != null ? revealCamPos : transform,
                    transform, // LookAt por defecto al objeto
                    revealDuration,
                    revealZoomAmount, // <--- Zoom específico de Reveal
                    revealZoomCurve,  // <--- Curva específica de Reveal
                    () => Save.MarkZoneRevealSeen(targetBuildIndex)
                );
            }
        }
        
        GameEventManager.Instance?.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }

    void CheckUnlockConditions()
    {
#if UNITY_EDITOR
        if (debugForceUnlock)
        {
            _isUnlocked = true;
            return;
        }
#endif

        if (isFirstZone)
        {
            _isUnlocked = true;
            return;
        }

        // A. ¿Boss vencido?
        bool bossDefeated = Save.IsLevelCompleted(requiredBossLevelIndex);

        // B. ¿Suficientes gemas globales?
        int currentGems = Save.GetGlobalGemCount();
        bool enoughGems = currentGems >= requiredTotalGems;

        _isUnlocked = bossDefeated && enoughGems;
    }

    private void Update()
    {
        if (_isUnlocked && _playerInside)
        {
            if (Input.GetButtonDown("Space"))
                EnterZone();
        }
    }

    void EnterZone()
    {
        // 1. Focus Entry (Animación de entrada)
        if (FocusManager.Instance != null)
        {
            Transform camPos = entryFocusPos != null ? entryFocusPos : transform;
            Transform lookAt = entryLookAt != null ? entryLookAt : transform;
            
            // Usamos los parámetros de ENTRY
            FocusManager.Instance.RequestObjectFocus(
                camPos, 
                lookAt, 
                entryDuration,
                entryZoomAmount, // <--- Zoom específico de Entry
                entryZoomCurve   // <--- Curva específica de Entry
            );
        }

        // 2. Load Scene
        if (uiManager != null)
            uiManager.GetComponent<SceneTransitionManager>().FadeInAndLoadScene(targetBuildIndex);
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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = true;

        if (_isUnlocked)
            GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.A);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }
}