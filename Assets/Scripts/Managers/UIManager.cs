using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    //Esentials
    private Player _player;
    private LevelManager levelManager;

    [Header("UI PAUSE")] [SerializeField] private GameObject _PausePanel;
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnRetry;
    [SerializeField] private Button _btnExit;

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

        _player._modelPlayer.SizeModify += UISetShootSlider;

        levelManager.OnPlaying += ResumeGame;
        levelManager.OnPause += PauseGame;

        levelManager.AddCollectible += UISetCollectibleCount;

        //Buttons OnClick
        _btnResume.onClick.AddListener(() => { levelManager.OnPlaying.Invoke(); });
        _btnRetry.onClick.AddListener(RetryLevel);
        _btnExit.onClick.AddListener(Exit);

        _btnNextLvlW.onClick.AddListener(ShowNextLvlPanel);
        _btnRetryW.onClick.AddListener(RetryLevel);

        _btnRetryL.onClick.AddListener(RetryLevel);

        ResetGems();
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
        Debug.Log("PAUSED");
        _PausePanel.SetActive(true);

        _WinPanel.SetActive(false);
        _LosePanel.SetActive(false);
        _NextLvlPanel.SetActive(false);
    }

    private void ResumeGame()
    {
        Debug.Log("PLAYING");
        _PausePanel.SetActive(false);
    }

    private void Exit()
    {
#if UNITY_EDITOR
        // Si estás en el editor, detiene la ejecución del juego
        UnityEditor.EditorApplication.isPlaying = false;
#else
                        // En una compilación, cierra la aplicación
                        Application.Quit();
#endif
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
        _PausePanel.SetActive(false);

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

    private void ResetGems()
    {
        _gemMaterial01.SetFloat("_IsPicked", 0);
        _gemMaterial02.SetFloat("_IsPicked", 0);
        _gemMaterial03.SetFloat("_IsPicked", 0);
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
        StartCoroutine(FadeIn(() => { _WinPanel.SetActive(true); }));
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