using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Animator _mummyUI;

    [FormerlySerializedAs("_interactionInput")] [FormerlySerializedAs("_tutorialInput")] [Header("UI Tutorial")] [SerializeField]
    private GameObject _interaction;
    [SerializeField] private Image _interactionBtn;

    [SerializeField] private Sprite btnA;
    [SerializeField] private Sprite btnY;

    [Header("UI PAUSE")] [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Material _pauseMaterial;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Material _optionsMaterial;
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnOptions;

    [SerializeField] private Button _btnExit;

    //(esto va junto al resto de botones del Pause / Options
    [SerializeField] private Button _btnBackOptions;

    private bool _isPaused;
    private bool _pauseCharging; // Controla que no se spamee la animación
    private const string PAUSE_FILL = "_Power";

    [Header("UI WIN")] [SerializeField] private GameObject _WinPanel;
    [SerializeField] private Button _btnRetryW;
    [SerializeField] private Button _btnMainMenuW;
    [SerializeField] private Button _btnNextLvlW;

    [Header("UI LOSE")] [SerializeField] private GameObject _LosePanel;
    [SerializeField] private Button _btnRetryL;
    [SerializeField] private Button _btnMainMenuL;

    [Header("UI NEXT LVL")] [SerializeField]
    private GameObject _NextLvlPanel;

    // Selectables iniciales por panel
    [Header("FIRST SELECTED")] [SerializeField]
    private Selectable _pauseFirstSelected; // btn Resume

    [SerializeField] private Selectable _optionsFirstSelected; // options

    // **NUEVOS CAMPOS para WIN/LOSE**
    [SerializeField] private Selectable _winFirstSelected;
    [SerializeField] private Selectable _loseFirstSelected;

    private float _fakeTimer = 5f;

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
        //Buttons OnClick
        AddButtonProps(_btnResume, () => GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false));
        AddButtonProps(_btnRetry, RetryLevel);
        AddButtonProps(_btnOptions, ShowOptionsPanel);
        AddButtonProps(_btnExit, GoToMainMenu);
        AddButtonProps(_btnBackOptions, BackFromOptions);

        AddButtonProps(_btnNextLvlW, ShowNextLvlPanel);
        AddButtonProps(_btnRetryW, RetryLevel);
        AddButtonProps(_btnMainMenuW, GoToMainMenu);

        AddButtonProps(_btnRetryL, RetryLevel);
        AddButtonProps(_btnMainMenuL, GoToMainMenu);

        // Inicializamos ambos materiales en 0
        _pauseMaterial.SetFloat(PAUSE_FILL, 0f);
        _optionsMaterial.SetFloat(PAUSE_FILL, 0f); // ** NUEVO **

        _postProcess = FindObjectOfType<Volume>();
        if (_postProcess != null)
            _postProcess.profile.TryGet(out _blur);
    }


    private void Update()
    {
        if (Input.GetButtonDown("Pause"))
            Toggle();
    }

    private static void SetSelected(Selectable selectable)
    {
        if (selectable == null) return;
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    private void Toggle()
    {
        if (_pauseCharging) return;

        bool willPause = !_isPaused;
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(willPause);
    }

    private void AddButtonProps(Button button, Action mainAction, params Action[] additionalActions)
    {
        button.onClick.AddListener(() =>
        {
            mainAction?.Invoke();

            if (additionalActions == null) return;
            foreach (var action in additionalActions)
            {
                action?.Invoke();
            }
        });
    }

    private void HandlePauseChanged(bool paused)
    {
        if (_isPaused == paused) return;
        _isPaused = paused;

        if (paused)
        {
            _optionsPanel.SetActive(false);
            _WinPanel.SetActive(false);
            _LosePanel.SetActive(false);
            _NextLvlPanel.SetActive(false);

            StartCoroutine(LoadPauseBandage());
        }
        else
        {
            _pausePanel.SetActive(false);
            _optionsPanel.SetActive(false);

            // ** CORRECCIÓN AQUÍ **
            // Reseteamos el material de opciones a 0 instantáneamente.
            // Al ocultarse el panel, no se ve el cambio brusco, pero asegura 
            // que la próxima vez empiece la animación desde 0.
            _optionsMaterial.SetFloat(PAUSE_FILL, 0f);

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            StartCoroutine(LoadPauseBandage());
        }
    }

    // Corutina del PAUSE
    private IEnumerator LoadPauseBandage()
    {
        _pauseCharging = true;

        if (_postProcess.profile.TryGet(out _blur))
            _blur.active = !_blur.active;

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

    // ** NUEVA CORUTINA: LoadOptionsBandage **
    // Funciona igual que la de Pausa pero maneja el material y panel de opciones
    private IEnumerator LoadOptionsBandage(bool open)
    {
        _pauseCharging = true; // Bloqueamos inputs mientras transiciona

        float startValue = open ? 0f : 1f;
        float endValue = open ? 1f : 0f;
        float elapsed = 0f;
        float duration = 0.5f;

        // Si cerramos opciones, primero ocultamos el panel para ver la animación de "vaciado"
        if (!open)
        {
            _optionsPanel.SetActive(false);
        }
        else
        {
            // Si abrimos opciones, ocultamos el panel de pausa para que no moleste atrás
            _pausePanel.SetActive(false);
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentValue = Mathf.Lerp(startValue, endValue, elapsed / duration);
            _optionsMaterial.SetFloat(PAUSE_FILL, currentValue);
            yield return null;
        }

        _optionsMaterial.SetFloat(PAUSE_FILL, endValue);
        _pauseCharging = false;

        if (open)
        {
            // Al terminar de cargar la animación, mostramos el panel de opciones
            _optionsPanel.SetActive(true);
            SetSelected(_optionsFirstSelected);
        }
        else
        {
            // Al terminar de descargar la animación, volvemos a mostrar el menú de pausa
            _pausePanel.SetActive(true);
            SetSelected(_pauseFirstSelected ?? _btnResume);
        }
    }

    private void GoToMainMenu()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false);
        Transition.FadeInAndLoadScene(0);
    }

    private void BackFromOptions()
    {
        // Ya no hacemos SetActive directo, llamamos a la corutina con 'false'
        if (!_pauseCharging)
            StartCoroutine(LoadOptionsBandage(false));
    }

    private void ShowOptionsPanel()
    {
        // Ya no hacemos SetActive directo, llamamos a la corutina con 'true'
        if (!_pauseCharging)
            StartCoroutine(LoadOptionsBandage(true));
    }

    private void ShowNextLvlPanel()
    {
        _NextLvlPanel.SetActive(true);

        _WinPanel.SetActive(false);
        _LosePanel.SetActive(false);
        _pausePanel.SetActive(false);

        StartCoroutine(LoadNextSceneWithPanel());
    }

    private IEnumerator LoadNextSceneWithPanel()
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

    public void ShowInteractionInput(bool value, buttonType button)
    {
        _interactionBtn.sprite = button switch
        {
            buttonType.A => btnA,
            buttonType.Y => btnY,
            _ => _interactionBtn.sprite
        };

        _interaction.SetActive(value);
    }

    public void Win(int index)
    {
        Transition.TriggerFadeIn(() =>
        {
            if (index >= Utils.MAX_LVLS)
                _btnNextLvlW.enabled = false;

            _WinPanel.SetActive(true);
            _mummyUI.SetTrigger("isWin");
            SetSelected(_winFirstSelected ?? _btnNextLvlW);
        });
    }

    public void Lose()
    {
        Transition.TriggerFadeIn(() =>
        {
            _LosePanel.SetActive(true);
            _mummyUI.SetTrigger("isLose");
            SetSelected(_loseFirstSelected ?? _btnRetryL);
        });
    }
}

public enum buttonType
{
    A,
    Y
}