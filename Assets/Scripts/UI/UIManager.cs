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

    [Header("UI Tutorial")]
    [SerializeField]private GameObject _tutorialInput;
    
    [Header("UI PAUSE")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Material _pauseMaterial;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;
    //(esto va junto al resto de botones del Pause / Options
    [SerializeField] private Button _btnBackOptions;
    
    private bool _isPaused;
    private bool _pauseCharging;
    private const string PAUSE_FILL = "_Power";
    
    [Header("UI WIN")] 
    [SerializeField] private GameObject _WinPanel;
    [SerializeField] private Button _btnRetryW;
    [SerializeField] private Button _btnMainMenuW;
    [SerializeField] private Button _btnNextLvlW;

    [Header("UI LOSE")] 
    [SerializeField] private GameObject _LosePanel;
    [SerializeField] private Button _btnRetryL;
    [SerializeField] private Button _btnMainMenuL;

    [Header("UI NEXT LVL")]
    [SerializeField] private GameObject _NextLvlPanel;
    
    // Selectables iniciales por panel
    [Header("FIRST SELECTED")]
    [SerializeField] private Selectable _pauseFirstSelected;    // btn Resume
    [SerializeField] private Selectable _optionsFirstSelected;  // options

    private float _fakeTimer = 5f;

    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();

    // ELIMINADO: El [Header("FADE")] y el fadeImage
    
    private DepthOfField _blur;
    private Volume _postProcess;


    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Register<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Register(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnTutorialPrompt.Register<bool>(ShowTutorialInput);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Lose);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(HandlePauseChanged);
        GameEventManager.Instance.levelEvents.OnTutorialPrompt.Unregister<bool>(ShowTutorialInput);
    }


    private void Start()
    {
        // ELIMINADO: StartCoroutine(FadeOut());
        // El script local SceneTransitionLocal lo hace solo en su propio Start().

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

        _pauseMaterial.SetFloat(PAUSE_FILL, 0f);

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
            //AudioManager.Instance.PlaySFX(NameSounds.SFX_Click);
            
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
            
            //Al cerrar pausa, limpiar selección
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            StartCoroutine(LoadPauseBandage());
        }
    }
    
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
            // MODIFICADO: Usar Time.unscaledDeltaTime para que funcione en pausa
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

    private void GoToMainMenu()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false);
        
        // MODIFICADO: Llama al script local _transition
        Transition.FadeInAndLoadScene(0);
    }
    
    private void BackFromOptions()
    {
        _optionsPanel.SetActive(false);
        _pausePanel.SetActive(true);

        // Devolver foco al panel de pausa
        SetSelected(_pauseFirstSelected ?? _btnResume);
    }
    
    private void ShowNextLvlPanel()
    {
        // TODAVÍA mostramos el panel "Next Lvl" como feedback
        _NextLvlPanel.SetActive(true); 

        _WinPanel.SetActive(false);
        _LosePanel.SetActive(false);
        _pausePanel.SetActive(false);

        // MODIFICADO: Llamamos a la nueva corutina de carga
        StartCoroutine(LoadNextSceneWithPanel());
    }
    
    // MODIFICADO: Esta corutina ahora es más simple
    private IEnumerator LoadNextSceneWithPanel()
    {
        // 1. Mantenemos el timer FAKE aquí solo porque
        // el panel "NextLvlPanel" está en pantalla.
        // Si no, el cambio sería instantáneo.
        yield return new WaitForSecondsRealtime(_fakeTimer); 

        // 2. Llama al manager para cargar
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        Transition.FadeInAndLoadScene(nextSceneIndex);
    }

    // ELIMINADO: LoadNextSceneAsync

    private void RetryLevel()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Raise(false);
        
        // MODIFICADO: Llama al script local _transition
        Transition.FadeInAndLoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ShowOptionsPanel()
    {
        _optionsPanel.SetActive(true);
        SetSelected(_optionsFirstSelected);
    } 

    // ELIMINADO: FadeIn y FadeOut (ahora están en SceneTransitionLocal)

    private void ShowTutorialInput(bool value) => _tutorialInput.SetActive(value);
    
    public void Win(int index)
    {
        // MODIFICADO: Usa el TriggerFadeIn del script local _transition
        Transition.TriggerFadeIn(() =>
        {
            //...
            if (index >= Utils.MAX_LVLS)
                _btnNextLvlW.enabled = false;

            _WinPanel.SetActive(true);
            _mummyUI.SetTrigger("isWin");

            //...
        });
    }

    public void Lose()
    {
        // MODIFICADO: Usa el TriggerFadeIn del script local _transition
        Transition.TriggerFadeIn(() =>
        {
            _LosePanel.SetActive(true);
            _mummyUI.SetTrigger("isLose");
            
            //...
        });
    }
}