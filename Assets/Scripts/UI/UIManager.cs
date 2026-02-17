using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using static Save;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Animator _mummyUI; 

    [Header("Configuración de Escenas")]
    [Tooltip("El índice de la escena que funciona como HUB / Selector.")]
    [SerializeField] private int selectorSceneIndex = 1;

    [Header("UI TUTORIAL / PROMPT")]
    [SerializeField] private GameObject _interaction;
    [SerializeField] private Image _interactionBtn;
    [SerializeField] private Sprite btnA;
    [SerializeField] private Sprite btnY;

    [Header("UI FOCUS MESSAGES")]
    [SerializeField] private GameObject _focusMessagePanel;
    [SerializeField] private TextMeshProUGUI _focusText;
    [SerializeField] private CanvasGroup _focusMessageCG; 
    [SerializeField] private float _fadeDuration = 0.3f;

    [Header("UI PAUSE / OPTIONS")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Material _pauseMaterial;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Material _optionsMaterial;
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry; 
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;   
    [SerializeField] private Button _btnBackOptions;
    [SerializeField] private Selectable _pauseFirstSelected;
    [SerializeField] private Selectable _optionsFirstSelected;

    [Header("UI END GAME (Resumen)")]
    [SerializeField] private GameObject _endGamePanel; 
    [SerializeField] private Button _btnEndGameRetry;    
    [SerializeField] private Button _btnEndGameMenu;     
    [SerializeField] private Button _btnEndGameNextLvl; 
    [SerializeField] private Selectable _winFirstSelected;  
    [SerializeField] private Selectable _loseFirstSelected; 
    [SerializeField] private Image[] _uiSlotsFills;     
    [SerializeField] private GameObject[] _gemIcons;    
    [SerializeField] private float _delayBeforeRefill = 2f;

    [Header("TRANSITION / LOADING")]
    [SerializeField] private GameObject _nextLvlTransitionPanel; // PanelNextLevel
    [SerializeField] private float _fakeTimer = 3f;

    // Variables internas
    private bool _isPaused;
    private bool _pauseCharging;
    private const string PAUSE_FILL = "_Power";
    private Coroutine _messageFadeRoutine;
    
    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();
    private DepthOfField _blur;
    private Volume _postProcess;

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Register<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Register(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnPrompt.Register<bool, buttonType>(ShowInteractionInput);
        GameEventManager.Instance.levelEvents.OnShowFocusMessage.Register<string, Color>(ShowFocusMessage);
        GameEventManager.Instance.levelEvents.OnHideFocusMessage.Register(HideFocusMessage);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnPrompt.Unregister<bool, buttonType>(ShowInteractionInput);
        GameEventManager.Instance.levelEvents.OnShowFocusMessage.Unregister<string, Color>(ShowFocusMessage);
        GameEventManager.Instance.levelEvents.OnHideFocusMessage.Unregister(HideFocusMessage);
    }

    private void Start()
    {
        // Botones de Pausa
        AddButtonProps(_btnResume, () => GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false));
        AddButtonProps(_btnRetry, RetryLevel);
        AddButtonProps(_btnOptions, ShowOptionsPanel);
        AddButtonProps(_btnExit, GoToMainMenu);
        AddButtonProps(_btnBackOptions, BackFromOptions);

        // Botones de Fin de Juego
        AddButtonProps(_btnEndGameRetry, RetryLevel); 
        AddButtonProps(_btnEndGameMenu, GoToMainMenu);
        AddButtonProps(_btnEndGameNextLvl, () => StartLoadingTransition(selectorSceneIndex));

        _pauseMaterial.SetFloat(PAUSE_FILL, 0f);
        _optionsMaterial.SetFloat(PAUSE_FILL, 0f);

        _postProcess = FindObjectOfType<Volume>();
        if (_postProcess != null) _postProcess.profile.TryGet(out _blur);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Pause")) Toggle();
    }

    // --- LÓGICA DE CARGA Y WIN ---

    public void Win(int index)
    {
        if (index >= 0)
        {
            // FLUJO DESDE NIVEL: FadeIn -> Mostrar resumen de victoria
            Transition.TriggerFadeIn(() => { SetupEndGamePanel(true, index); });
        }
        else
        {
            // FLUJO DESDE SELECTOR: FadeIn -> Mostrar pantalla de carga (Momia) -> Cargar Nivel
            int targetScene = Mathf.Abs(index);
            Transition.TriggerFadeIn(() => { StartLoadingTransition(targetScene); });
        }
    }

    public void Lose()
    {
        Transition.TriggerFadeIn(() => { SetupEndGamePanel(false, -1); });
    }

    private void StartLoadingTransition(int targetScene)
    {
        // Aseguramos que el panel de resumen esté apagado
        _endGamePanel.SetActive(false);
        
        // Activamos la pantalla de carga (PanelNextLevel)
        if (_nextLvlTransitionPanel != null) _nextLvlTransitionPanel.SetActive(true);
        
        StartCoroutine(LoadingRoutine(targetScene));
    }

    private IEnumerator LoadingRoutine(int targetScene)
    {
        yield return new WaitForSecondsRealtime(_fakeTimer);
        Transition.FadeInAndLoadScene(targetScene);
    }

    private void SetupEndGamePanel(bool isWin, int levelIndex)
    {
        _endGamePanel.SetActive(true);
        _btnEndGameRetry.gameObject.SetActive(true);
        _btnEndGameMenu.gameObject.SetActive(true);
        _btnEndGameNextLvl.gameObject.SetActive(isWin); 

        if (isWin)
        {
            _mummyUI.SetTrigger("Win");
            SetSelected(_winFirstSelected ?? _btnEndGameNextLvl);
        }
        else
        {
            _mummyUI.SetTrigger("Lose");
            SetSelected(_loseFirstSelected ?? _btnEndGameRetry);
        }

        StartCoroutine(EndGameUIFlow());
    }

    // --- ANIMACIONES DE UI (Barras y Gemas) ---

    private IEnumerator EndGameUIFlow()
    {
        foreach (var gem in _gemIcons) if (gem != null) gem.SetActive(false);
        foreach (var slot in _uiSlotsFills) if (slot != null) slot.fillAmount = 0f;

        yield return new WaitForSecondsRealtime(_delayBeforeRefill);

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
        CheckAndShowCollectedGems();
    }

    private void CheckAndShowCollectedGems()
    {
        for (int i = 0; i < _gemIcons.Length; i++)
        {
            if (_gemIcons[i] != null && WasGemPicked(i + 1)) _gemIcons[i].SetActive(true);
        }
    }

    // --- MENSAJES DE FOCO ---

    private void ShowFocusMessage(string message, Color color)
    {
        if (_focusText == null) return;
        _focusText.text = message;
        _focusText.color = color;
        if (_messageFadeRoutine != null) StopCoroutine(_messageFadeRoutine);
        _messageFadeRoutine = StartCoroutine(FadeMessage(1f));
    }

    private void HideFocusMessage()
    {
        if (_messageFadeRoutine != null) StopCoroutine(_messageFadeRoutine);
        _messageFadeRoutine = StartCoroutine(FadeMessage(0f));
    }

    private IEnumerator FadeMessage(float targetAlpha)
    {
        if (_focusMessageCG == null) { _focusMessagePanel.SetActive(targetAlpha > 0); yield break; }
        if (targetAlpha > 0) _focusMessagePanel.SetActive(true);
        float startAlpha = _focusMessageCG.alpha;
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            _focusMessageCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
            yield return null;
        }
        _focusMessageCG.alpha = targetAlpha;
        if (targetAlpha <= 0) _focusMessagePanel.SetActive(false);
    }

    // --- PAUSA Y NAVEGACIÓN ---

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

    private void HandlePauseChanged(bool paused)
    {
        if (_isPaused == paused) return;
        _isPaused = paused;
        if (paused)
        {
            _optionsPanel.SetActive(false);
            _endGamePanel.SetActive(false); 
        }
        else
        {
            _pausePanel.SetActive(false);
            _optionsPanel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        StartCoroutine(LoadPauseBandage());
    }
    
    private IEnumerator LoadPauseBandage()
    {
        _pauseCharging = true;
        if (_blur != null) _blur.active = _isPaused;
        float start = _pauseMaterial.GetFloat(PAUSE_FILL);
        float end = _isPaused ? 1f : 0f;
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            _pauseMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(start, end, elapsed / 0.5f));
            yield return null;
        }
        _pauseCharging = false;
        if (_isPaused) { _pausePanel.SetActive(true); SetSelected(_pauseFirstSelected ?? _btnResume); }
        _pauseMaterial.SetFloat(PAUSE_FILL, end);
    }

    private void Toggle() { if (!_pauseCharging) GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(!_isPaused); }
    private void ShowOptionsPanel() { if (!_pauseCharging) StartCoroutine(LoadOptionsBandage(true)); }
    private void BackFromOptions() { if (!_pauseCharging) StartCoroutine(LoadOptionsBandage(false)); }
    
    private IEnumerator LoadOptionsBandage(bool open)
    {
        _pauseCharging = true;
        float start = open ? 0f : 1f;
        float end = open ? 1f : 0f;
        float elapsed = 0f;
        if (!open) _optionsPanel.SetActive(false); else _pausePanel.SetActive(false);
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            _optionsMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(start, end, elapsed / 0.5f));
            yield return null;
        }
        _optionsMaterial.SetFloat(PAUSE_FILL, end);
        _pauseCharging = false;
        if (open) { _optionsPanel.SetActive(true); SetSelected(_optionsFirstSelected); }
        else { _pausePanel.SetActive(true); SetSelected(_pauseFirstSelected ?? _btnResume); }
    }

    public void ShowInteractionInput(bool value, buttonType button)
    {
        if (_interactionBtn == null) return;
        _interactionBtn.sprite = button switch { buttonType.A => btnA, buttonType.Y => btnY, _ => _interactionBtn.sprite };
        _interaction.SetActive(value);
    }

    private void AddButtonProps(Button button, Action action)
    {
        if (button != null) button.onClick.AddListener(() => action?.Invoke());
    }

    private static void SetSelected(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}

public enum buttonType
{
    A,
    Y
}