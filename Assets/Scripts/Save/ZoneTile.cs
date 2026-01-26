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

    [Header("Focus Reveal (Animación de Desbloqueo)")]
    [SerializeField] private Transform revealCamPos;
    [SerializeField] private float revealDuration = 3.0f;

    [Header("Focus Entry (Animación al Entrar)")]
    [SerializeField] private Transform entryFocusPos;
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

    [Tooltip("Si es true, la zona estará abierta ignorando Boss y Gemas.")]
    [SerializeField] private bool debugForceUnlock;

    [ContextMenu("Borrar Datos de ESTA Zona")]
    void ClearZoneData()
    {
        // Borrar el "Visto"
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
        // Si la zona está desbloqueada y NO la hemos visto, pedimos foco.
        // Pasa el targetBuildIndex como prioridad de ordenamiento.
        if (!Save.IsZoneRevealSeen(targetBuildIndex))
        {
            if (FocusManager.Instance != null)
            {
                FocusManager.Instance.RequestRevealFocus(
                    targetBuildIndex, // Prioridad en la cola
                    revealCamPos != null ? revealCamPos : transform,
                    transform,
                    revealDuration,
                    () => Save.MarkZoneRevealSeen(targetBuildIndex)
                );
            }
        }
        
        GameEventManager.Instance?.levelEvents.OnTutorialPrompt.Raise(false, buttonType.A);
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

        // Debug info solo si está bloqueado
        if (!_isUnlocked && _playerInside) // Solo loguear si intenta entrar o al debuggear
        {
             // (Opcional) Log para entender por qué está cerrado
        }
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
            FocusManager.Instance.RequestObjectFocus(camPos, lookAt, entryDuration);
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
            GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(true, buttonType.A);
        else
        {
#if UNITY_EDITOR
            Debug.Log($"[ZoneTile] Bloqueado. BossLv{requiredBossLevelIndex} completado: {Save.IsLevelCompleted(requiredBossLevelIndex)}. Gemas: {Save.GetGlobalGemCount()}/{requiredTotalGems}");
#endif
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(false, buttonType.A);
    }
}