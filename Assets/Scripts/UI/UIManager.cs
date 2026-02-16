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

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Necesario para los mensajes en pantalla

// Tu lógica de guardado estática
using static Save;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Animator _mummyUI; 

    [Header("UI TUTORIAL")]
    [SerializeField] private GameObject _interaction;
    [SerializeField] private Image _interactionBtn;
    [SerializeField] private Sprite btnA;
    [SerializeField] private Sprite btnY;

    [Header("UI FOCUS MESSAGES (Opcional)")]
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

    [Header("UI END GAME (Unified)")]
    [SerializeField] private GameObject _endGamePanel; 
    [SerializeField] private Button _btnEndGameRetry;    
    [SerializeField] private Button _btnEndGameMenu;     
    [SerializeField] private Button _btnEndGameNextLvl;  
    [SerializeField] private Selectable _winFirstSelected;  
    [SerializeField] private Selectable _loseFirstSelected; 

    [SerializeField] private Image[] _uiSlotsFills;     
    [SerializeField] private GameObject[] _gemIcons;    
    [SerializeField] private float _delayBeforeRefill = 2f;

    [Header("NEXT LEVEL TRANSITION")]
    [SerializeField] private GameObject _nextLvlTransitionPanel; 
    private float _fakeTimer = 5f;

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
        
        // Registro de eventos de Focus
        GameEventManager.Instance.levelEvents.OnShowFocusMessage.Register<string, Color>(ShowFocusMessage);
        GameEventManager.Instance.levelEvents.OnHideFocusMessage.Register(HideFocusMessage);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnPrompt.Unregister<bool, buttonType>(ShowInteractionInput);
        
        // Unregister de eventos de Focus
        GameEventManager.Instance.levelEvents.OnShowFocusMessage.Unregister<string, Color>(ShowFocusMessage);
        GameEventManager.Instance.levelEvents.OnHideFocusMessage.Unregister(HideFocusMessage);
    }

    private void Start()
    {
        AddButtonProps(_btnResume, () => GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false));
        AddButtonProps(_btnRetry, RetryLevel);
        AddButtonProps(_btnOptions, ShowOptionsPanel);
        AddButtonProps(_btnExit, GoToMainMenu);
        AddButtonProps(_btnBackOptions, BackFromOptions);

        AddButtonProps(_btnEndGameRetry, RetryLevel); 
        AddButtonProps(_btnEndGameMenu, GoToMainMenu);
        AddButtonProps(_btnEndGameNextLvl, ShowNextLvlTransition);

        _pauseMaterial.SetFloat(PAUSE_FILL, 0f);
        _optionsMaterial.SetFloat(PAUSE_FILL, 0f);

        _postProcess = FindObjectOfType<Volume>();
        if (_postProcess != null) _postProcess.profile.TryGet(out _blur);
        
        // Inicializar panel de mensajes oculto
        if (_focusMessageCG != null) _focusMessageCG.alpha = 0;
        if (_focusMessagePanel != null) _focusMessagePanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Pause")) Toggle();
    }

    // --- LÓGICA DE MENSAJES DE FOCO ---

    private void ShowFocusMessage(string message, Color color)
    {
        if (_focusMessagePanel == null || _focusText == null) return;

        _focusText.text = message;
        _focusText.color = color;
        
        if (_messageFadeRoutine != null) StopCoroutine(_messageFadeRoutine);
        _messageFadeRoutine = StartCoroutine(FadeMessage(1f));
    }

    private void HideFocusMessage()
    {
        if (_focusMessagePanel == null) return;
        
        if (_messageFadeRoutine != null) StopCoroutine(_messageFadeRoutine);
        _messageFadeRoutine = StartCoroutine(FadeMessage(0f));
    }

    private IEnumerator FadeMessage(float targetAlpha)
    {
        if (_focusMessageCG == null)
        {
            _focusMessagePanel.SetActive(targetAlpha > 0);
            yield break;
        }

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

    // --- LOGICA UNIFICADA WIN / LOSE ---

    public void Win(int index)
    {
        Transition.TriggerFadeIn(() => { SetupEndGamePanel(true, index); });
    }

    public void Lose()
    {
        Transition.TriggerFadeIn(() => { SetupEndGamePanel(false, -1); });
    }

    private void SetupEndGamePanel(bool isWin, int levelIndex)
    {
        _endGamePanel.SetActive(true);
        _btnEndGameRetry.gameObject.SetActive(true);
        _btnEndGameMenu.gameObject.SetActive(true);

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        bool hasNextLevel = nextSceneIndex < SceneManager.sceneCountInBuildSettings;
        bool showNextButton = isWin && hasNextLevel; 
        _btnEndGameNextLvl.gameObject.SetActive(showNextButton);

        if (isWin)
        {
            _mummyUI.SetTrigger("Win");
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
            int gemNum = i + 1; 
            if (_gemIcons[i] != null && WasGemPicked(gemNum))
            {
                _gemIcons[i].SetActive(true);
            }
        }
    }

    // --- NAVEGACIÓN Y UTILIDADES ---

    private void ShowNextLvlTransition()
    {
        if(_nextLvlTransitionPanel != null) _nextLvlTransitionPanel.SetActive(true);
        _endGamePanel.SetActive(false); 
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
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(!_isPaused);
    }
    
    private void HandlePauseChanged(bool paused)
    {
        if (_isPaused == paused) return;
        _isPaused = paused;

        if (paused)
        {
            _optionsPanel.SetActive(false);
            _endGamePanel.SetActive(false); 
            if(_nextLvlTransitionPanel) _nextLvlTransitionPanel.SetActive(false);
        }
        else
        {
            _pausePanel.SetActive(false);
            _optionsPanel.SetActive(false);
            _optionsMaterial.SetFloat(PAUSE_FILL, 0f);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        StartCoroutine(LoadPauseBandage());
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
            _pauseMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(startValue, endValue, elapsed / 0.5f));
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

        if (!open) _optionsPanel.SetActive(false);
        else _pausePanel.SetActive(false);

        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            _optionsMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(startValue, endValue, elapsed / 0.5f));
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