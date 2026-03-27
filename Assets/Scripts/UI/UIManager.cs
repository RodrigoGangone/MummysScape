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

    [Header("UI CONTEXTUAL")]
    [SerializeField] private GameObject _interaction;
    [SerializeField] private Image _interactionBtn;
    [SerializeField] private Sprite btnA;
    [SerializeField] private Sprite btnY;

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
    [SerializeField] private GameObject _nextLvlTransitionPanel;
    [SerializeField] private float _fakeTimer = 3f;

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
        GameEventManager.Instance.levelEvents.OnContextUIChanged.Register<ContextUIData>(HandleContextUIChanged);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnContextUIChanged.Unregister<ContextUIData>(HandleContextUIChanged);
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
        AddButtonProps(_btnEndGameNextLvl, () => StartLoadingTransition(selectorSceneIndex));

        _pauseMaterial.SetFloat(PAUSE_FILL, 0f);
        _optionsMaterial.SetFloat(PAUSE_FILL, 0f);

        if (_interaction != null) _interaction.SetActive(false);

        if (_focusMessageCG != null)
        {
            _focusMessageCG.alpha = 0f;
        }

        if (_focusMessagePanel != null)
        {
            _focusMessagePanel.SetActive(false);
        }

        _postProcess = FindObjectOfType<Volume>();
        if (_postProcess != null)
            _postProcess.profile.TryGet(out _blur);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Pause") && !PlayerLock.Instance.IsLocked)
            Toggle();
    }

    // -------------------------
    // WIN / LOSE / LOADING
    // -------------------------

    public void Win(int index)
    {
        if (index >= 0)
        {
            Transition.TriggerFadeIn(() =>
            {
                SetupEndGamePanel(true, index);
            });
        }
        else
        {
            int targetScene = Mathf.Abs(index);
            Transition.TriggerFadeIn(() =>
            {
                StartLoadingTransition(targetScene);
            });
        }
    }

    public void Lose()
    {
        Transition.TriggerFadeIn(() =>
        {
            SetupEndGamePanel(false, -1);
        });
    }

    private void StartLoadingTransition(int targetScene)
    {
        _endGamePanel.SetActive(false);

        if (_nextLvlTransitionPanel != null)
            _nextLvlTransitionPanel.SetActive(true);

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
            if (_mummyUI != null)
                _mummyUI.SetTrigger("Win");

            SetSelected(_winFirstSelected ?? _btnEndGameNextLvl);
        }
        else
        {
            if (_mummyUI != null)
                _mummyUI.SetTrigger("Lose");

            SetSelected(_loseFirstSelected ?? _btnEndGameRetry);
        }

        Transition.TriggerFadeOut(null);
        StartCoroutine(EndGameUIFlow());
    }

    private IEnumerator EndGameUIFlow()
    {
        foreach (var gem in _gemIcons)
            if (gem != null)
                gem.SetActive(false);

        foreach (var slot in _uiSlotsFills)
            if (slot != null)
                slot.fillAmount = 0f;

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
            if (_gemIcons[i] != null && WasGemPicked(i + 1))
                _gemIcons[i].SetActive(true);
        }
    }

    // -------------------------
    // CONTEXT UI
    // -------------------------

    private string ResolveContextText(ContextUIData data)
    {
        switch (data.MessageType)
        {
            case ContextMessageType.Interact:
                return "PRESIONE PARA INTERACTUAR";

            case ContextMessageType.Enter:
                return "PRESIONE PARA ENTRAR";

            case ContextMessageType.ReplayTutorial:
                return "PRESIONE PARA VER TUTORIAL";

            case ContextMessageType.CancelReplay:
                return "PRESIONE PARA CANCELAR";

            case ContextMessageType.Custom:
                return data.CustomText;

            case ContextMessageType.None:
            default:
                return string.Empty;
        }
    }
    
    private void HandleContextUIChanged(ContextUIData data)
    {
        if (!data.Visible)
        {
            HideContextUI();
            return;
        }

        ShowContextUI(data);
    }

    private void ShowContextUI(ContextUIData data)
    {
        ShowInteractionInput(data.UseButton, data.Button);

        if (_focusText != null)
        {
            _focusText.text = ResolveContextText(data);
            _focusText.color = data.TextColor;
        }

        if (_messageFadeRoutine != null)
            StopCoroutine(_messageFadeRoutine);

        _messageFadeRoutine = StartCoroutine(FadeMessage(1f));
    }

    private void HideContextUI()
    {
        if (_messageFadeRoutine != null)
            StopCoroutine(_messageFadeRoutine);

        _messageFadeRoutine = StartCoroutine(FadeMessage(0f));
    }

    private IEnumerator FadeMessage(float targetAlpha)
    {
        if (_focusMessageCG == null)
        {
            if (_focusMessagePanel != null)
                _focusMessagePanel.SetActive(targetAlpha > 0f);
            yield break;
        }

        if (targetAlpha > 0f && _focusMessagePanel != null)
            _focusMessagePanel.SetActive(true);

        float startAlpha = _focusMessageCG.alpha;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _focusMessageCG.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
            yield return null;
        }

        _focusMessageCG.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            ShowInteractionInput(false, default);

            if (_focusMessagePanel != null)
                _focusMessagePanel.SetActive(false);
        }
    }

    public void ShowInteractionInput(bool value, buttonType button)
    {
        if (_interaction != null)
            _interaction.SetActive(value);

        if (!value || _interactionBtn == null)
            return;

        _interactionBtn.sprite = button switch
        {
            buttonType.A => btnA,
            buttonType.Y => btnY,
            _ => _interactionBtn.sprite
        };
    }

    // -------------------------
    // PAUSA / NAVEGACIÓN
    // -------------------------

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

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        StartCoroutine(LoadPauseBandage());
    }

    private IEnumerator LoadPauseBandage()
    {
        _pauseCharging = true;

        if (_blur != null)
            _blur.active = _isPaused;

        float startPause = _pauseMaterial.GetFloat(PAUSE_FILL);
        float startOptions = _optionsMaterial.GetFloat(PAUSE_FILL);

        float end = _isPaused ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / 0.5f;

            _pauseMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(startPause, end, t));

            if (!_isPaused)
            {
                _optionsMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(startOptions, 0f, t));
            }

            yield return null;
        }

        _pauseCharging = false;

        if (_isPaused)
        {
            _pausePanel.SetActive(true);
            SetSelected(_pauseFirstSelected ?? _btnResume);
        }

        _pauseMaterial.SetFloat(PAUSE_FILL, end);

        if (!_isPaused)
            _optionsMaterial.SetFloat(PAUSE_FILL, 0f);
    }

    private void Toggle()
    {
        if (!_pauseCharging)
            GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(!_isPaused);
    }

    private void ShowOptionsPanel()
    {
        if (!_pauseCharging)
            StartCoroutine(LoadOptionsBandage(true));
    }

    private void BackFromOptions()
    {
        if (!_pauseCharging)
            StartCoroutine(LoadOptionsBandage(false));
    }

    private IEnumerator LoadOptionsBandage(bool open)
    {
        _pauseCharging = true;

        float start = open ? 0f : 1f;
        float end = open ? 1f : 0f;
        float elapsed = 0f;

        if (!open)
            _optionsPanel.SetActive(false);
        else
            _pausePanel.SetActive(false);

        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            _optionsMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(start, end, elapsed / 0.5f));
            yield return null;
        }

        _optionsMaterial.SetFloat(PAUSE_FILL, end);
        _pauseCharging = false;

        if (open)
        {
            _optionsPanel.SetActive(true);
            SetSelected(_optionsFirstSelected);
        }
        else
        {
            _pausePanel.SetActive(true);
            SetSelected(_pauseFirstSelected ?? _btnResume);
        }
    }

    private void AddButtonProps(Button button, Action action)
    {
        if (button != null)
            button.onClick.AddListener(() => action?.Invoke());
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