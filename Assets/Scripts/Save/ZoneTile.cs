using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necesario para los textos de las gemas

[RequireComponent(typeof(Collider))]
public class ZoneTile : MonoBehaviour
{
    [Header("Configuración de Zona")]
    [Tooltip("A qué escena (Build Index) viaja este portal.")]
    [SerializeField] private int targetBuildIndex;
    [SerializeField] private bool isFirstZone;

    [Header("Requisitos de Desbloqueo")]
    [Tooltip("El nivel (índice) del Boss anterior que debe estar completado.")]
    [SerializeField] private int requiredBossLevelIndex;
    [Tooltip("Cantidad TOTAL de gemas requeridas (Suma global de todo el juego).")]
    [SerializeField] private int requiredTotalGems;

    [Header("Referencias Visuales - Escenario")]
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private Material lockedMaterial;

    [Header("Referencias Visuales - UI Flotante")]
    [Tooltip("El objeto padre (Canvas o Panel) que contiene los iconos.")]
    [SerializeField] private GameObject infoCanvasPanel; 
    [SerializeField] private TextMeshProUGUI gemCountText; // Texto ej: "5/10"
    [SerializeField] private Image bossIconImage;          // Imagen del Escorpión
    [SerializeField] private Color statusLockedColor = Color.red;
    [SerializeField] private Color statusUnlockedColor = Color.green;

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

    private bool _isUnlocked;
    private bool _playerInside;

    void Start()
    {
        // 0. Ocultar la UI flotante al inicio
        if (infoCanvasPanel != null) 
            infoCanvasPanel.SetActive(false);

        // --- DEBUG: Resetear estado 'Seen' ---
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
                FocusManager.Instance.RequestRevealFocus(
                    targetBuildIndex, // Prioridad
                    revealCamPos != null ? revealCamPos : transform,
                    transform, // LookAt por defecto al objeto
                    revealDuration,
                    revealZoomAmount, 
                    revealZoomCurve,  
                    () => Save.MarkZoneRevealSeen(targetBuildIndex)
                );
            }
        }
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
            if (Input.GetButtonDown("Space")) // O tu input system preferido
                EnterZone();
        }
    }

    // --- Lógica de UI Flotante ---
    
    void UpdateFloatingUI()
    {
        if (infoCanvasPanel == null) return;

        // 1. Actualizar Gemas
        int currentGems = Save.GetGlobalGemCount();
        bool hasEnoughGems = currentGems >= requiredTotalGems;

        if (gemCountText != null)
        {
            gemCountText.text = $"{currentGems} / {requiredTotalGems}";
            gemCountText.color = hasEnoughGems ? statusUnlockedColor : statusLockedColor;
        }

        // 2. Actualizar Boss (Escorpión)
        bool bossDefeated = Save.IsLevelCompleted(requiredBossLevelIndex);

        if (bossIconImage != null)
        {
            // Opcional: Cambiar color del icono según estado
            bossIconImage.color = bossDefeated ? statusUnlockedColor : statusLockedColor;
            
            // Opcional: Si prefieres que se vea gris/transparente si no está vencido:
            // bossIconImage.color = bossDefeated ? Color.white : new Color(1,1,1, 0.3f);
        }
    }

    // --- Interacción y Triggers ---

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = true;

        // Refrescar estado por si consiguió gemas recién
        CheckUnlockConditions(); 

        // Mostrar UI Flotante
        if (infoCanvasPanel != null)
        {
            UpdateFloatingUI();
            infoCanvasPanel.SetActive(true);
        }

        // Mostrar Prompt de botón "A" (Space) SOLO si está desbloqueado
        if (_isUnlocked)
        {
            GameEventManager.Instance.levelEvents.OnPrompt.Raise(true, buttonType.A);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;

        // Ocultar UI Flotante
        if (infoCanvasPanel != null)
            infoCanvasPanel.SetActive(false);

        // Ocultar Prompt
        GameEventManager.Instance.levelEvents.OnPrompt.Raise(false, buttonType.A);
    }

    void EnterZone()
    {
        // 1. Focus Entry (Animación de entrada)
        if (FocusManager.Instance != null)
        {
            Transform camPos = entryFocusPos != null ? entryFocusPos : transform;
            Transform lookAt = entryLookAt != null ? entryLookAt : transform;
            
            FocusManager.Instance.RequestObjectFocus(
                camPos, 
                lookAt, 
                entryDuration,
                entryZoomAmount, 
                entryZoomCurve   
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
            // Evitamos cambiar el material del Canvas/UI si por error son hijos del renderer
            if (r.GetComponent<CanvasRenderer>() != null) continue;

            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = lockedMaterial;
            r.materials = mats;
        }
    }
}