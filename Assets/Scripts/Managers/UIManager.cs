using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    //Esentials
    private Player _player;
    private LevelManager levelManager;

    [Header("UI PAUSE")] [SerializeField] private GameObject _pausePanel;

    [SerializeField] private List<GameObject> _btnsPause;

    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnExit;
    [SerializeField] private Material _pauseMaterial;

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

    [SerializeField] private float _fakeTimer = 3f;

    [Header("FADE")] [SerializeField] private Image fadeImage;

    [Header("HOUR GLASS")] [SerializeField]
    private Material _HourgalssBandage01;

    [SerializeField] private Material _HourgalssBandage02;

    [SerializeField] private Material _sandTimer01;
    [SerializeField] private Material _sandTimer02;

    [SerializeField] private Material _gemMaterial01;
    [SerializeField] private Material _gemMaterial02;
    [SerializeField] private Material _gemMaterial03;

    [SerializeField] private Animator _hourglassAnimator;


    private float targetOffset1;
    private float targetOffset2;
    private float targetOffset3;

    private float _targetOffset1;
    private float _targetOffset2;
    private float _targetOffset3;
    private float _fillSpeed = 1f;

    void Start()
    {
        _player = FindObjectOfType<Player>();
        levelManager = FindObjectOfType<LevelManager>();

        _player.SizeModify += UISetShootSlider;

        levelManager.OnPlaying += ResumeGame;
        levelManager.OnPause += PauseGame;

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

        _pauseMaterial.SetFloat("_Fill", 0f); // Asegurar que se complete la transición

        ValidateGems();
        UpdateTargetOffsets(); // Inicializar valores correctos
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
        StartCoroutine(LoadPauseBandage());

        Debug.Log("PLAYING");
    }

    private IEnumerator LoadPauseBandage()
    {
        PauseCharging = true;

        float startValue = _pauseMaterial.GetFloat("_Fill"); // Obtener el valor actual del material
        float endValue = (startValue == 1f) ? 0f : 1f; // Determinar si debe ir a 1 o a 0
        float elapsed = 0f;

        StartCoroutine(CascadeButtons(_btnsPause));

        //TODO: LLEVAR ESTA CORRUTINA ABAJO DEL WHILE DESPUES DE ENTREGAR AL BUILD EL 10/10
        
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;

            float currentValue = Mathf.Lerp(startValue, endValue, elapsed / 0.5f);

            _pauseMaterial.SetFloat("_Fill", currentValue); // Ajusta según la propiedad de tu shader
            yield return null;
        }
        
        PauseCharging = false;
        
        _pauseMaterial.SetFloat("_Fill", endValue); // Asegurar que se complete la transición
    }

    private IEnumerator CascadeButtons(List<GameObject> buttons)
    {
        foreach (var btn in buttons)
        {
            btn.SetActive(!btn.activeSelf);
            yield return new WaitForSeconds(0.1f);
        }

        buttons.Reverse();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void UISetTimerDeath(float currentTimer, float maxtime)
    {
        _targetOffset3 = Mathf.Clamp01(currentTimer / maxtime);
    }

    public void UISetShootSlider()
    {
        UpdateTargetOffsets(); // Actualizar valores de offset según las vendas actuales
    }

    private void UpdateTargetOffsets()
    {
        int currentBandage = _player.CurrentBandageStock;

        _targetOffset1 = currentBandage;
        _targetOffset2 = currentBandage;

        if (currentBandage <= 0)
        {
            _hourglassAnimator.SetBool("isSkull", true);
        }
        else
        {
            _hourglassAnimator.SetBool("isSkull", false);
        }
    }

    private void UpdateMaterialOffsets()
    {
        _HourgalssBandage01.SetFloat("_Offset",
            Mathf.MoveTowards(_HourgalssBandage01.GetFloat("_Offset"), _targetOffset1, _fillSpeed * Time.deltaTime));
        _HourgalssBandage02.SetFloat("_Offset",
            Mathf.MoveTowards(_HourgalssBandage02.GetFloat("_Offset"), _targetOffset2, _fillSpeed * Time.deltaTime));

        _sandTimer01.SetFloat("_Fill",
            Mathf.MoveTowards(_sandTimer01.GetFloat("_Fill"), _targetOffset3, _fillSpeed * Time.deltaTime));
        _sandTimer02.SetFloat("_Fill",
            Mathf.MoveTowards(_sandTimer02.GetFloat("_Fill"), _targetOffset3, _fillSpeed * Time.deltaTime));
    }

    public void UISetCollectibleCount(CollectibleNumber num)
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
        float duration = 3f;
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
        float duration = 3f;
        float time = 0f;

        while (time < duration)
        {
            alpha = Mathf.Lerp(1, 0, time / duration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            time += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, 0f);

        //TODO: Aca se podria hacer un action que active el script del player para que no se mueva mientras esta el fade
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
        }));
    }

    public void Lose()
    {
        StartCoroutine(FadeIn(() => { _LosePanel.SetActive(true); }));
    }

    private void OnEnable()
    {
        StartCoroutine(FadeOut());
    }
}