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

[Serializable]
public class SceneConfig
{
    [Tooltip("El índice de la escena que funciona como HUB / Selector.")]
    public int SelectorSceneIndex = 1;
}

[Serializable]
public class ContextUIRefs
{
    [Header("Root")]
    public CanvasGroup CanvasGroup;

    [Header("Content")]
    public Image InteractionBtn;
    public TextMeshProUGUI FocusText;

    [Header("Sprites")]
    public Sprite BtnA;
    public Sprite BtnY;

    [Header("Animation")]
    public float FadeDuration = 0.3f;
}

[Serializable]
public class PauseUIRefs
{
    [Header("Panels")]
    public GameObject PausePanel;
    public GameObject OptionsPanel;

    [Header("Materials")]
    public Material PauseMaterial;
    public Material OptionsMaterial;

    [Header("Buttons")]
    public Button BtnResume;
    public Button BtnRetry;
    public Button BtnOptions;
    public Button BtnExit;
    public Button BtnBackOptions;

    [Header("Navigation")]
    public Selectable PauseFirstSelected;
    public Selectable OptionsFirstSelected;
}

[Serializable]
public class EndGameUIRefs
{
    [Header("Panel")]
    public GameObject EndGamePanel;

    [Header("Buttons")]
    public Button BtnEndGameRetry;
    public Button BtnEndGameMenu;
    public Button BtnEndGameNextLvl;

    [Header("Navigation")]
    public Selectable WinFirstSelected;
    public Selectable LoseFirstSelected;

    [Header("Summary")]
    public Image[] UiSlotsFills;
    public GameObject Gems;
    public float DelayBeforeRefill = 2f;
}

[Serializable]
public class LoadingUIRefs
{
    public GameObject NextLvlTransitionPanel;
    public float FakeTimer = 3f;
}

public class UIManager : MonoBehaviour
{
    [Header("GENERAL")]
    [SerializeField] private Animator _mummyUI;

    [Header("CONFIGURACIÓN DE ESCENAS")]
    [SerializeField] private SceneConfig _sceneConfig;

    [Header("UI CONTEXTUAL")]
    [SerializeField] private ContextUIRefs _contextUI;

    [Header("UI PAUSE / OPTIONS")]
    [SerializeField] private PauseUIRefs _pauseUI;

    [Header("UI END GAME (Resumen)")]
    [SerializeField] private EndGameUIRefs _endGameUI;

    [Header("TRANSITION / LOADING")]
    [SerializeField] private LoadingUIRefs _loadingUI;

    private bool _isPaused;
    private bool _pauseCharging;
    private const string PAUSE_FILL = "_Power";
    private Coroutine _messageFadeRoutine;

    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();
    private DepthOfField _blur;
    private Volume _postProcess;
    
    private void Start()
    {
        AddButtonProps(_pauseUI.BtnResume, () => GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false));
        AddButtonProps(_pauseUI.BtnRetry, RetryLevel);
        AddButtonProps(_pauseUI.BtnOptions, ShowOptionsPanel);
        AddButtonProps(_pauseUI.BtnExit, GoToMainMenu);
        AddButtonProps(_pauseUI.BtnBackOptions, BackFromOptions);

        AddButtonProps(_endGameUI.BtnEndGameRetry, RetryLevel);
        AddButtonProps(_endGameUI.BtnEndGameMenu, GoToMainMenu);
        AddButtonProps(_endGameUI.BtnEndGameNextLvl, LoadNextLevel);
        if (_pauseUI.PauseMaterial != null)
            _pauseUI.PauseMaterial.SetFloat(PAUSE_FILL, 0f);

        if (_pauseUI.OptionsMaterial != null)
            _pauseUI.OptionsMaterial.SetFloat(PAUSE_FILL, 0f);

        if (_contextUI.CanvasGroup != null)
        {
            _contextUI.CanvasGroup.alpha = 0f;
            _contextUI.CanvasGroup.gameObject.SetActive(false);
        }

        if (_contextUI.InteractionBtn != null)
            _contextUI.InteractionBtn.gameObject.SetActive(false);

        _postProcess = FindObjectOfType<Volume>();
        if (_postProcess != null)
            _postProcess.profile.TryGet(out _blur);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Pause") && !PlayerLock.Instance.IsLocked)
            Toggle();

        if (Input.GetButtonDown("Drop") && _isPaused && !_pauseCharging)
        {
            if (_pauseUI.OptionsPanel != null && _pauseUI.OptionsPanel.activeSelf)
            {
                BackFromOptions();
            }
            else if (_pauseUI.PausePanel != null && _pauseUI.PausePanel.activeSelf)
            {
                Toggle(); 
            }
        }
    }

    #region Win - Lose - Loading
    
    private void Win(int index)
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

    private void Lose()
    {
        Transition.TriggerFadeIn(() =>
        {
            SetupEndGamePanel(false, -1);
        });
    }

    private void StartLoadingTransition(int targetScene)
    {
        if (_endGameUI.EndGamePanel != null)
            _endGameUI.EndGamePanel.SetActive(false);

        if (_loadingUI.NextLvlTransitionPanel != null)
            _loadingUI.NextLvlTransitionPanel.SetActive(true);

        StartCoroutine(LoadingRoutine(targetScene));
    }

    private IEnumerator LoadingRoutine(int targetScene)
    {
        yield return new WaitForSecondsRealtime(_loadingUI.FakeTimer);
        Transition.FadeInAndLoadScene(targetScene);
    }

    private void SetupEndGamePanel(bool isWin, int levelIndex)
    {
        if (_endGameUI.EndGamePanel != null)
            _endGameUI.EndGamePanel.SetActive(true);

        if (_endGameUI.BtnEndGameRetry != null)
            _endGameUI.BtnEndGameRetry.gameObject.SetActive(true);

        if (_endGameUI.BtnEndGameMenu != null)
            _endGameUI.BtnEndGameMenu.gameObject.SetActive(true);

        if (_endGameUI.BtnEndGameNextLvl != null)
            _endGameUI.BtnEndGameNextLvl.gameObject.SetActive(isWin);

        if (isWin)
        {
            if (_mummyUI != null)
                _mummyUI.SetTrigger("Win");

            SetSelected(_endGameUI.WinFirstSelected != null
                ? _endGameUI.WinFirstSelected
                : _endGameUI.BtnEndGameNextLvl);
        }
        else
        {
            if (_mummyUI != null)
                _mummyUI.SetTrigger("Lose");

            SetSelected(_endGameUI.LoseFirstSelected != null
                ? _endGameUI.LoseFirstSelected
                : _endGameUI.BtnEndGameRetry);
        }

        Transition.TriggerFadeOut(null);
        StartCoroutine(EndGameUIFlow());
    }

    private IEnumerator EndGameUIFlow()
    {
        _endGameUI.Gems.SetActive(false);
        
        foreach (var slot in _endGameUI.UiSlotsFills)
        {
            if (slot != null)
                slot.fillAmount = 0f;
        }

        yield return new WaitForSecondsRealtime(_endGameUI.DelayBeforeRefill);

        foreach (var slot in _endGameUI.UiSlotsFills)
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
        
        _endGameUI.Gems.SetActive(true);
    }

    private void LoadNextLevel()
    {
        // Obtenemos el índice de la escena actual y le sumamos 1
        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
    
        // Obtenemos la cantidad total de escenas en el Build Settings
        int totalScenesInBuild = SceneManager.sceneCountInBuildSettings;

        // Evaluamos: si el próximo índice existe, vamos a ese. Si nos pasamos del límite, vamos al HUB.
        int targetScene = (nextBuildIndex < totalScenesInBuild) 
            ? nextBuildIndex 
            : _sceneConfig.SelectorSceneIndex;

        StartLoadingTransition(targetScene);
    }
    
    #endregion

    #region Context UI
    
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
        Debug.Log($"UI -> {data.MessageType}");

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

        if (_contextUI.FocusText != null)
        {
            _contextUI.FocusText.text = ResolveContextText(data);
            _contextUI.FocusText.color = data.TextColor;
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
        if (_contextUI.CanvasGroup == null)
            yield break;

        GameObject root = _contextUI.CanvasGroup.gameObject;

        if (targetAlpha > 0f)
            root.SetActive(true);

        float startAlpha = _contextUI.CanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < _contextUI.FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _contextUI.CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _contextUI.FadeDuration);
            yield return null;
        }

        _contextUI.CanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            if (_contextUI.InteractionBtn != null)
                _contextUI.InteractionBtn.gameObject.SetActive(false);

            root.SetActive(false);
        }
    }

    private void ShowInteractionInput(bool value, ButtonType button)
    {
        if (_contextUI.InteractionBtn != null)
            _contextUI.InteractionBtn.gameObject.SetActive(value);

        if (!value || _contextUI.InteractionBtn == null)
            return;

        _contextUI.InteractionBtn.sprite = button switch
        {
            ButtonType.A => _contextUI.BtnA,
            ButtonType.Y => _contextUI.BtnY,
            _ => _contextUI.InteractionBtn.sprite
        };
    }
    
    #endregion

    #region Pause - Navigation
    
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
            if (_pauseUI.OptionsPanel != null)
                _pauseUI.OptionsPanel.SetActive(false);

            if (_endGameUI.EndGamePanel != null)
                _endGameUI.EndGamePanel.SetActive(false);
        }
        else
        {
            if (_pauseUI.PausePanel != null)
                _pauseUI.PausePanel.SetActive(false);

            if (_pauseUI.OptionsPanel != null)
                _pauseUI.OptionsPanel.SetActive(false);

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

        float startPause = _pauseUI.PauseMaterial != null ? _pauseUI.PauseMaterial.GetFloat(PAUSE_FILL) : 0f;
        float startOptions = _pauseUI.OptionsMaterial != null ? _pauseUI.OptionsMaterial.GetFloat(PAUSE_FILL) : 0f;

        float end = _isPaused ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / 0.5f;

            if (_pauseUI.PauseMaterial != null)
                _pauseUI.PauseMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(startPause, end, t));

            if (!_isPaused && _pauseUI.OptionsMaterial != null)
                _pauseUI.OptionsMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(startOptions, 0f, t));

            yield return null;
        }

        _pauseCharging = false;

        if (_isPaused)
        {
            if (_pauseUI.PausePanel != null)
                _pauseUI.PausePanel.SetActive(true);

            SetSelected(_pauseUI.PauseFirstSelected != null
                ? _pauseUI.PauseFirstSelected
                : _pauseUI.BtnResume);
        }

        if (_pauseUI.PauseMaterial != null)
            _pauseUI.PauseMaterial.SetFloat(PAUSE_FILL, end);

        if (!_isPaused && _pauseUI.OptionsMaterial != null)
            _pauseUI.OptionsMaterial.SetFloat(PAUSE_FILL, 0f);
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
        {
            if (_pauseUI.OptionsPanel != null)
                _pauseUI.OptionsPanel.SetActive(false);
        }
        else
        {
            if (_pauseUI.PausePanel != null)
                _pauseUI.PausePanel.SetActive(false);
        }

        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;

            if (_pauseUI.OptionsMaterial != null)
                _pauseUI.OptionsMaterial.SetFloat(PAUSE_FILL, Mathf.Lerp(start, end, elapsed / 0.5f));

            yield return null;
        }

        if (_pauseUI.OptionsMaterial != null)
            _pauseUI.OptionsMaterial.SetFloat(PAUSE_FILL, end);

        _pauseCharging = false;

        if (open)
        {
            if (_pauseUI.OptionsPanel != null)
                _pauseUI.OptionsPanel.SetActive(true);

            SetSelected(_pauseUI.OptionsFirstSelected);
        }
        else
        {
            if (_pauseUI.PausePanel != null)
                _pauseUI.PausePanel.SetActive(true);

            SetSelected(_pauseUI.PauseFirstSelected != null
                ? _pauseUI.PauseFirstSelected
                : _pauseUI.BtnResume);
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
    
    #endregion
    
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
}

public enum ButtonType { A, Y }