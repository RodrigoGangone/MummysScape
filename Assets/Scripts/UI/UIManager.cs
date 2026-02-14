using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// Tu lógica de guardado estática
using static Save;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Animator _mummyUI; // El animator que cambia el titulo de Win/Lose

    [Header("UI TUTORIAL")]
    [SerializeField] private GameObject _interaction;
    [SerializeField] private Image _interactionBtn;
    [SerializeField] private Sprite btnA;
    [SerializeField] private Sprite btnY;

    [Header("UI PAUSE / OPTIONS")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Material _pauseMaterial;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Material _optionsMaterial;
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry; // Botón del menú de pausa
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;   // Botón del menú de pausa
    [SerializeField] private Button _btnBackOptions;
    [SerializeField] private Selectable _pauseFirstSelected;
    [SerializeField] private Selectable _optionsFirstSelected;

    // --- SECCIÓN UNIFICADA END GAME (WIN / LOSE) ---
    [Header("UI END GAME (Unified)")]
    [SerializeField] private GameObject _endGamePanel; // Un solo panel para ambos casos
    
    // Botones compartidos
    [SerializeField] private Button _btnEndGameRetry;    // Reintenta (aparece en Win y Lose)
    [SerializeField] private Button _btnEndGameMenu;     // Menu (aparece en Win y Lose)
    [SerializeField] private Button _btnEndGameNextLvl;  // Siguiente (SOLO en Win)

    // Selección inicial para navegación con Gamepad/Teclado
    [SerializeField] private Selectable _winFirstSelected;  // Usualmente btnNextLvl
    [SerializeField] private Selectable _loseFirstSelected; // Usualmente btnRetry

    // Elementos visuales de las Gemas
    [SerializeField] private Image[] _uiSlotsFills;     // Fondo/Barra (Image Type: Filled)
    [SerializeField] private GameObject[] _gemIcons;    // Iconos de gemas (Se activan si se recogen)
    [SerializeField] private float _delayBeforeRefill = 2f;

    [Header("NEXT LEVEL TRANSITION")]
    [SerializeField] private GameObject _nextLvlTransitionPanel; // Panel negro o fade extra si lo usas
    private float _fakeTimer = 5f;

    // Variables internas
    private bool _isPaused;
    private bool _pauseCharging;
    private const string PAUSE_FILL = "_Power";
    
    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();
    private DepthOfField _blur;
    private Volume _postProcess;

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Register<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Register(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnPrompt.Register<bool, buttonType>(ShowInteractionInput);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnPrompt.Unregister<bool, buttonType>(ShowInteractionInput);
    }

    private void Start()
    {
        // --- PAUSE BUTTONS ---
        AddButtonProps(_btnResume, () => GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false));
        AddButtonProps(_btnRetry, RetryLevel);
        AddButtonProps(_btnOptions, ShowOptionsPanel);
        AddButtonProps(_btnExit, GoToMainMenu);
        AddButtonProps(_btnBackOptions, BackFromOptions);

        // --- END GAME BUTTONS (Unificados) ---
        // Retry sirve tanto para Win como para Lose
        AddButtonProps(_btnEndGameRetry, RetryLevel); 
        // Menu sirve tanto para Win como para Lose
        AddButtonProps(_btnEndGameMenu, GoToMainMenu);
        // Next Level solo sirve para Win
        AddButtonProps(_btnEndGameNextLvl, ShowNextLvlTransition);

        // Inicializar materiales
        _pauseMaterial.SetFloat(PAUSE_FILL, 0f);
        _optionsMaterial.SetFloat(PAUSE_FILL, 0f);

        _postProcess = FindObjectOfType<Volume>();
        if (_postProcess != null) _postProcess.profile.TryGet(out _blur);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Pause")) Toggle();
    }

    // ---------------------------------------------------------
    // LOGICA UNIFICADA WIN / LOSE
    // ---------------------------------------------------------

    public void Win(int index)
    {
        Transition.TriggerFadeIn(() =>
        {
            SetupEndGamePanel(true, index);
        });
    }

    public void Lose()
    {
        Transition.TriggerFadeIn(() =>
        {
            SetupEndGamePanel(false, -1);
        });
    }

    private void SetupEndGamePanel(bool isWin, int levelIndex)
    {
        _endGamePanel.SetActive(true);

        // 1. Configurar Botones
        _btnEndGameRetry.gameObject.SetActive(true);
        _btnEndGameMenu.gameObject.SetActive(true);

        // SOLUCIÓN: Comparamos si el siguiente índice existe en el Build Settings
        // SceneManager.sceneCountInBuildSettings nos da el total de escenas.
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        bool hasNextLevel = nextSceneIndex < SceneManager.sceneCountInBuildSettings;

        // Solo mostramos el botón si ganamos Y hay una escena a la cual ir
        bool showNextButton = isWin && hasNextLevel; 
        _btnEndGameNextLvl.gameObject.SetActive(showNextButton);

        // 2. Configurar Animator...
        if (isWin)
        {
            _mummyUI.SetTrigger("Win");
            // Si no hay siguiente nivel, seleccionamos el menú por defecto
            Selectable first = showNextButton ? _btnEndGameNextLvl : _btnEndGameMenu;
            SetSelected(first);
        }
        else
        {
            _mummyUI.SetTrigger("Lose");
            SetSelected(_loseFirstSelected ?? _btnEndGameRetry);
        }

        StartCoroutine(EndGameUIFlow());
    }
    private IEnumerator EndGameUIFlow()
    {
        // A. Reset Inicial
        foreach (var gem in _gemIcons) if (gem != null) gem.SetActive(false);
        foreach (var slot in _uiSlotsFills) if (slot != null) slot.fillAmount = 0f;

        // B. Espera
        yield return new WaitForSecondsRealtime(_delayBeforeRefill);

        // C. Animación de llenado de Slots (Image Filled)
        foreach (var slot in _uiSlotsFills)
        {
            if (slot == null) continue;
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                slot.fillAmount = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            slot.fillAmount = 1f;
        }

        // D. Mostrar Gemas ganadas
        CheckAndShowCollectedGems();
    }

    private void CheckAndShowCollectedGems()
    {
        for (int i = 0; i < _gemIcons.Length; i++)
        {
            int gemNum = i + 1; 
            bool isPicked = WasGemPicked(gemNum); // Tu método estático de Save

            if (_gemIcons[i] != null && isPicked)
            {
                _gemIcons[i].SetActive(true);
            }
        }
    }

    // ---------------------------------------------------------
    // NAVEGACIÓN Y UTILIDADES
    // ---------------------------------------------------------

    private void ShowNextLvlTransition()
    {
        if(_nextLvlTransitionPanel != null) 
            _nextLvlTransitionPanel.SetActive(true);
            
        _endGamePanel.SetActive(false); // Ocultamos el panel actual
        
        StartCoroutine(LoadNextSceneRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        yield return new WaitForSecondsRealtime(_fakeTimer);
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        Transition.FadeInAndLoadScene(nextSceneIndex);
    }

    private void RetryLevel()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false);
        Transition.FadeInAndLoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMainMenu()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false);
        Transition.FadeInAndLoadScene(0);
    }

    // ... (Mantén aquí tus métodos Toggle, HandlePauseChanged, LoadPauseBandage, LoadOptionsBandage, AddButtonProps, SetSelected, ShowInteractionInput) ...
    // Para no alargar demasiado la respuesta, asumo que copias los métodos que no cambiaron de tu script original.
    
    // Helper duplicado para referencia (ya lo tienes en tu script original):
    private void AddButtonProps(Button button, Action mainAction, params Action[] additionalActions)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
            mainAction?.Invoke();
            if (additionalActions != null)
                foreach (var action in additionalActions) action?.Invoke();
        });
    }

    private static void SetSelected(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
    
    private void Toggle() 
    {
        if (_pauseCharging) return;
        bool willPause = !_isPaused;
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(willPause);
    }
    
    private void HandlePauseChanged(bool paused)
    {
        if (_isPaused == paused) return;
        _isPaused = paused;

        if (paused)
        {
            _optionsPanel.SetActive(false);
            _endGamePanel.SetActive(false); // Aseguramos que el EndGame no moleste
            if(_nextLvlTransitionPanel) _nextLvlTransitionPanel.SetActive(false);
            
            StartCoroutine(LoadPauseBandage());
        }
        else
        {
            _pausePanel.SetActive(false);
            _optionsPanel.SetActive(false);
            _optionsMaterial.SetFloat(PAUSE_FILL, 0f); // Reset instantáneo
            
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(LoadPauseBandage());
        }
    }
    
    private IEnumerator LoadPauseBandage()
    {
        _pauseCharging = true;
        if (_postProcess.profile.TryGet(out _blur)) _blur.active = !_blur.active;

        var startValue = _pauseMaterial.GetFloat(PAUSE_FILL);
        var endValue = (startValue == 1f) ? 0f : 1f;
        var elapsed = 0f;

        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            var currentValue = Mathf.Lerp(startValue, endValue, elapsed / 0.5f);
            _pauseMaterial.SetFloat(PAUSE_FILL, currentValue);
            yield return null;
        }

        _pauseCharging = false;
        if (endValue == 1f)
        {
            _pausePanel.SetActive(true);
            SetSelected(_pauseFirstSelected ?? _btnResume);
        }
        _pauseMaterial.SetFloat(PAUSE_FILL, endValue);
    }
    
    private void ShowOptionsPanel() { if (!_pauseCharging) StartCoroutine(LoadOptionsBandage(true)); }
    private void BackFromOptions() { if (!_pauseCharging) StartCoroutine(LoadOptionsBandage(false)); }
    
    private IEnumerator LoadOptionsBandage(bool open)
    {
        _pauseCharging = true;
        float startValue = open ? 0f : 1f;
        float endValue = open ? 1f : 0f;
        float elapsed = 0f;
        float duration = 0.5f;

        if (!open) _optionsPanel.SetActive(false);
        else _pausePanel.SetActive(false);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _optionsMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(startValue, endValue, elapsed / duration));
            yield return null;
        }
        _optionsMaterial.SetFloat(PAUSE_FILL, endValue);
        _pauseCharging = false;

        if (open) { _optionsPanel.SetActive(true); SetSelected(_optionsFirstSelected); }
        else { _pausePanel.SetActive(true); SetSelected(_pauseFirstSelected ?? _btnResume); }
    }
    
    public void ShowInteractionInput(bool value, buttonType button)
    {
        _interactionBtn.sprite = button switch { buttonType.A => btnA, buttonType.Y => btnY, _ => _interactionBtn.sprite };
        _interaction.SetActive(value);
    }
}

public enum buttonType
{
    A,
    Y
}