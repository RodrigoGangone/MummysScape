using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Animator _mummyUI;

    [Header("UI PAUSE")] 
    
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Material _pauseMaterial;
    
    [SerializeField] private GameObject _optionsPanel;

    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;
    
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

    private float _fakeTimer = 5f;

    [Header("FADE")] 
    
    [SerializeField] private Image fadeImage;
    private float _fillSpeed = 1f;
    private DepthOfField _blur;
    private Volume _postProcess;


    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Register(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Register(Lose);
        GameEventManager.Instance.levelEvents.OnPause.Register(Pause);
        GameEventManager.Instance.levelEvents.OnResume.Register(Resume);
        //GameEventManager.Instance.levelEvents.OnRespawn.Register();
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnWin.Unregister(Win);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Lose);
        GameEventManager.Instance.levelEvents.OnPause.Unregister(Pause);
        GameEventManager.Instance.levelEvents.OnResume.Unregister(Resume);
        //GameEventManager.Instance.levelEvents.OnRespawn.Register();
    }

    private void Start()
    {
        StartCoroutine(FadeOut());

        //Buttons OnClick
        AddButtonProps(_btnResume, GameEventManager.Instance.levelEvents.OnResume.Raise);
        AddButtonProps(_btnRetry, RetryLevel);
        AddButtonProps(_btnOptions, ShowOptionsPanel);
        AddButtonProps(_btnExit, GoToMainMenu);

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
        if (Input.GetKeyDown(KeyCode.Escape) && !_pauseCharging)
        {
            if (_isPaused)
                GameEventManager.Instance.levelEvents.OnResume.Raise();
            else
                GameEventManager.Instance.levelEvents.OnPause.Raise();
        }
    }

    
    private void AddButtonProps(Button button, Action mainAction, params Action[] additionalActions)
    {
        button.onClick.AddListener(() =>
        {
            //AudioManager.Instance.PlaySFX(NameSounds.SFX_Click);

            //Accion principal
            mainAction?.Invoke();

            //Acciones secundarias
            if (additionalActions == null) return;
            foreach (var action in additionalActions)
            {
                action?.Invoke();
            }
        });
    }

    private void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        StartCoroutine(LoadPauseBandage());

        _optionsPanel.SetActive(false);
        _WinPanel.SetActive(false);
        _LosePanel.SetActive(false);
        _NextLvlPanel.SetActive(false);
    }

    private void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        
        _pausePanel.SetActive(false);
        _optionsPanel.SetActive(false);

        StartCoroutine(LoadPauseBandage());
    }

    private IEnumerator LoadPauseBandage()
    {
        _pauseCharging = true;

        if (_postProcess.profile.TryGet(out _blur))
            _blur.active = !_blur.active;

        var startValue = _pauseMaterial.GetFloat(PAUSE_FILL); // Obtener el valor actual del material
        var endValue = (startValue == 1f) ? 0f : 1f; // Determinar si debe ir a 1 o a 0
        var elapsed = 0f;
        
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;

            var currentValue = Mathf.Lerp(startValue, endValue, elapsed / 0.5f);

            _pauseMaterial.SetFloat(PAUSE_FILL, currentValue); // Ajusta según la propiedad de tu shader
            yield return null;
        }

        _pauseCharging = false;

        if (endValue == 1f)
            _pausePanel.SetActive(true);

        _pauseMaterial.SetFloat(PAUSE_FILL, endValue); // Asegurar que se complete la transición
    }

    private void GoToMainMenu() => SceneManager.LoadScene(0);

    private void ShowNextLvlPanel()
    {
        _NextLvlPanel.SetActive(true);

        _WinPanel.SetActive(false);
        _LosePanel.SetActive(false);
        _pausePanel.SetActive(false);

        StartCoroutine(LoadNextSceneAsync());
    }
    
    private IEnumerator LoadNextSceneAsync()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneIndex);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f) // Esperar hasta que la carga haya terminado al 90%
            {
                //Carga fake de "X" segundos luego cambiar de escena
                //yield return new WaitForSeconds(_fakeTimer);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void RetryLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    private void ShowOptionsPanel() => _optionsPanel.SetActive(true);

    private IEnumerator FadeIn(Action onFadeComplete)
    {
        Color color = fadeImage.color;
        float alpha = 0f;
        float duration = 1f;
        float time = 0f;

        while (time < duration)
        {
            alpha = Mathf.Lerp(0, 1, time / duration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            time += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, 1f);

        //Ejecuto un action al terminar el FadeIn
        onFadeComplete?.Invoke();
    }

    private IEnumerator FadeOut()
    {
        Color color = fadeImage.color;
        float alpha = 1f;
        float duration = 1f;
        float time = 0f;

        while (time < duration)
        {
            alpha = Mathf.Lerp(1, 0, time / duration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            time += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, 0f);
    }

    public void Win()
    {
        StartCoroutine(FadeIn(() =>
        {
            //Si llego a la cantidad de niveles max no muestro boton de sig nivel
            //TODO: MOSTRAR OTRA PANTALLA QUE NO TENGA LA DE SIGUIENTE NIVEL
            if (SceneManager.GetActiveScene().buildIndex >= Utils.MAX_LVLS)
                _btnNextLvlW.enabled = false;

            _WinPanel.SetActive(true);
            _mummyUI.SetTrigger("isWin");

            //AudioManager.Instance.PlayMusic(NameSounds.Music_Win);
            //AudioManager.Instance.MuteAllActiveSFX();
        }));
    }

    public void Lose()
    {
        StartCoroutine(FadeIn(() =>
        {
            _LosePanel.SetActive(true);
            _mummyUI.SetTrigger("isLose");

            //AudioManager.Instance.PlayMusic(NameSounds.Music_Lose);
            //AudioManager.Instance.MuteAllActiveSFX();
        }));
    }
}