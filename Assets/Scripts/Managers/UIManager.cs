using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class UIManager : MonoBehaviour
{
    //Esentials
    private Player _player;
    private LevelManager levelManager;

    [SerializeField] private Animator _mummyUI;

    [Header("UI PAUSE")] [SerializeField] private GameObject _pausePanel;

    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnExit;
    [SerializeField] private Material _pauseMaterial;
    private const string PAUSE_FILL = "_Power";

    public static bool PauseCharging;

    [Header("UI WIN")] [SerializeField] private GameObject _WinPanel;
    [SerializeField] private Button _btnRetryW;
    [SerializeField] private Button _btnMainMenuW;
    [SerializeField] private Button _btnNextLvlW;

    [Header("UI LOSE")] [SerializeField] private GameObject _LosePanel;
    [SerializeField] private Button _btnRetryL;
    [SerializeField] private Button _btnMainMenuL;

    [Header("UI NEXT LVL")] // Panel con animacion de momia y carga asincronica de nivel
    [SerializeField]
    private GameObject _NextLvlPanel;

    private int _currentTip;
    [SerializeField] private Image _tips;
    [SerializeField] private List<Sprite> _tipsNextLevel = new();

    [SerializeField] private float _fakeTimer = 3f;

    [Header("FADE")] [SerializeField] private Image fadeImage;

    [Header("HOUR GLASS")] [SerializeField]
    private Material _hourglassBandage01;

    [SerializeField] private Material _hourglassBandage02;
    [SerializeField] private Animator _hourglassAnimator;
    [SerializeField] private Transform _hourglassScale;

    private Vector3 _hourglassOriginalScale;

    private float _frecuencyHourglassScale;
    private float _timeHourglassScale;
    private float _waitTimeBeat = 3f;

    private Coroutine _beatCoroutineHandler;
    private Coroutine _beatCoroutine;

    [SerializeField] private Material _sandTimer01;
    [SerializeField] private Material _sandTimer02;

    [SerializeField] private Material _gemMaterial01;
    [SerializeField] private Material _gemMaterial02;
    [SerializeField] private Material _gemMaterial03;

    private float targetOffset1;
    private float targetOffset2;
    private float targetOffset3;

    private float _targetOffset1;
    private float _targetOffset2;
    private float _targetOffset3;

    private float _fillSpeed = 1f;

    private DepthOfField _blur;
    private Volume _postProcess;

    void Start()
    {
        _player = FindObjectOfType<Player>();
        levelManager = FindObjectOfType<LevelManager>();

        _player.SizeModify += UpdateTargetOffsets;

        levelManager.OnPlaying += ResumeGame;
        levelManager.OnPause += PauseGame;

        levelManager.DeathTimer += () =>
        {
            if (_beatCoroutineHandler != null) return;

            _waitTimeBeat = 3f;
            _beatCoroutineHandler = StartCoroutine(HourglassBeatHandler());
        };

        levelManager.AddCollectible += UISetCollectibleCount;

        //Buttons OnClick
        _btnResume.onClick.AddListener(() => { levelManager.OnPlaying.Invoke(); });
        _btnRetry.onClick.AddListener(RetryLevel);
        _btnExit.onClick.AddListener(GoToMainMenu);

        _btnNextLvlW.onClick.AddListener(ShowNextLvlPanel);
        _btnRetryW.onClick.AddListener(RetryLevel);
        _btnMainMenuW.onClick.AddListener(GoToMainMenu);

        _btnRetryL.onClick.AddListener(RetryLevel);
        _btnMainMenuL.onClick.AddListener(GoToMainMenu);

        _pauseMaterial.SetFloat(PAUSE_FILL, 0f); // Asegurar que se complete la transición

        _postProcess = FindObjectOfType<Volume>();

        _hourglassOriginalScale = _hourglassScale.transform.localScale;

        ValidateGems();
        UpdateTargetOffsets(); // Inicializar valores correctos
    }

    IEnumerator HourglassBeatHandler()
    {
        while (true)
        {
            if (levelManager.isBusy || levelManager._currentLevelState.Equals(LevelState.Pause))
            {
                yield return null;
                continue;
            }

            yield return _beatCoroutine = StartCoroutine(Beat(1.1f, 0.1f));

            yield return _beatCoroutine = StartCoroutine(Beat(1.1f, 0.1f));

            yield return new WaitForSeconds(_waitTimeBeat);

            _waitTimeBeat *= 0.9f;
        }
    }

    IEnumerator Beat(float targetScale, float duration)
    {
        float elapsed = 0f;
        Vector3 initialScale = _hourglassOriginalScale;
        Vector3 targetScaleVector = new Vector3(targetScale, targetScale, targetScale);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            _hourglassScale.localScale =
                Vector3.Lerp(initialScale, targetScaleVector, Mathf.SmoothStep(0f, 1f, progress));

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            _hourglassScale.localScale =
                Vector3.Lerp(targetScaleVector, initialScale, Mathf.SmoothStep(0f, 1f, progress));

            yield return null;
        }

        _hourglassScale.localScale = _hourglassOriginalScale;
    }

    private void Update()
    {
        UISetTimerDeath(levelManager._currentTimeDeath,
            levelManager._maxTimeDeath); //TODO: ESTO DEBERIA ESTAR SEPARADO DEL LEVEL MANAGER

        UpdateMaterialOffsets(); //TODO: ESTO SE DEBERIA DETENER CUANDO LAS VENDAS ESTAN LLENAS
    }

    // Método para pausar el juego y activar el PausePanel
    private void PauseGame()
    {
        StartCoroutine(LoadPauseBandage());

        Debug.Log("PAUSED");

        _WinPanel.SetActive(false);
        _LosePanel.SetActive(false);
        _NextLvlPanel.SetActive(false);
    }

    private void ResumeGame()
    {
        _pausePanel.SetActive(false);

        StartCoroutine(LoadPauseBandage());
    }

    private IEnumerator LoadPauseBandage()
    {
        PauseCharging = true;

        if (_postProcess.profile.TryGet(out _blur))
            _blur.active = !_blur.active;

        float startValue = _pauseMaterial.GetFloat(PAUSE_FILL); // Obtener el valor actual del material
        float endValue = (startValue == 1f) ? 0f : 1f; // Determinar si debe ir a 1 o a 0
        float elapsed = 0f;

        //TODO: LLEVAR ESTA CORRUTINA ABAJO DEL WHILE DESPUES DE ENTREGAR AL BUILD EL 10/10

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;

            float currentValue = Mathf.Lerp(startValue, endValue, elapsed / 0.5f);

            _pauseMaterial.SetFloat(PAUSE_FILL, currentValue); // Ajusta según la propiedad de tu shader
            yield return null;
        }

        PauseCharging = false;

        if (endValue == 1f)
            _pausePanel.SetActive(true);

        _pauseMaterial.SetFloat(PAUSE_FILL, endValue); // Asegurar que se complete la transición
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void UISetTimerDeath(float currentTimer, float maxtime)
    {
        _targetOffset3 = Mathf.Clamp01(currentTimer / maxtime);

        if (currentTimer <= 0)
            _hourglassAnimator.SetBool("isDeath", true);
    }

    private void UpdateTargetOffsets()
    {
        int currentBandage = _player.CurrentBandageStock;

        _targetOffset1 = currentBandage;
        _targetOffset2 = currentBandage;

        if (currentBandage > 0)
        {
            if (_beatCoroutineHandler != null)
            {
                StopCoroutine(_beatCoroutineHandler);
                _beatCoroutineHandler = null;
            }

            if (_beatCoroutine != null)
            {
                StopCoroutine(_beatCoroutine);
                _beatCoroutine = null;
            }

            _hourglassScale.localScale = _hourglassOriginalScale; // Restablecer tamaño original
        }
    }

    private void UpdateMaterialOffsets()
    {
        _hourglassBandage01.SetFloat("_Offset",
            Mathf.MoveTowards(_hourglassBandage01.GetFloat("_Offset"), _targetOffset1, _fillSpeed * Time.deltaTime));
        _hourglassBandage02.SetFloat("_Offset",
            Mathf.MoveTowards(_hourglassBandage02.GetFloat("_Offset"), _targetOffset2, _fillSpeed * Time.deltaTime));

        _sandTimer01.SetFloat("_Fill",
            Mathf.MoveTowards(_sandTimer01.GetFloat("_Fill"), _targetOffset3, _fillSpeed * Time.deltaTime));
        _sandTimer02.SetFloat("_Fill",
            Mathf.MoveTowards(_sandTimer02.GetFloat("_Fill"), _targetOffset3, _fillSpeed * Time.deltaTime));
    }

    private void UISetCollectibleCount(CollectibleNumber num)
    {
        switch (num)
        {
            case CollectibleNumber.One:
                _gemMaterial01.SetFloat("_IsPicked", 1);
                break;
            case CollectibleNumber.Two:
                _gemMaterial02.SetFloat("_IsPicked", 1);
                break;
            case CollectibleNumber.Three:
                _gemMaterial03.SetFloat("_IsPicked", 1);
                break;
        }
    }

    private void ShowNextLvlPanel()
    {
        _NextLvlPanel.SetActive(true);

        //TODO: CAMBIAR POR PLAYER PREFS PARA QUE NO MUESTRE SIEMPRE EL MISMO TIP
        
        int _currentTip = PlayerPrefs.GetInt("currentTip", 0);
        
        _tips.sprite = _tipsNextLevel[_currentTip];
        _tips.SetNativeSize();
        
        _currentTip = (_currentTip + 1) % _tipsNextLevel.Count;
        
        PlayerPrefs.SetInt("currentTip", _currentTip);
        PlayerPrefs.Save();

        _WinPanel.SetActive(false);
        _LosePanel.SetActive(false);
        _pausePanel.SetActive(false);

        //TODO: Activar animacion de momia
        //AnimationMummy.play();

        //Carga asincrona
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
                yield return new WaitForSeconds(_fakeTimer);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void RetryLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    private void ValidateGems()
    {
        _gemMaterial01.SetFloat("_IsPicked", 0);
        _gemMaterial02.SetFloat("_IsPicked", 0);
        _gemMaterial03.SetFloat("_IsPicked", 0);

        foreach (var level in LevelManagerJson.Levels)
        {
            if (level.level.Equals(SceneManager.GetActiveScene().buildIndex))
            {
                foreach (var collectible in level.collectibleNumbers)
                {
                    UISetCollectibleCount(collectible);
                }
            }
        }
    }

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
        }));
    }

    public void Lose()
    {
        StartCoroutine(FadeIn(() =>
        {
            _LosePanel.SetActive(true);
            _mummyUI.SetTrigger("isLose");
        }));
    }

    private void OnEnable()
    {
        StartCoroutine(FadeOut());
    }
}