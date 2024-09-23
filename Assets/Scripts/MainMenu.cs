using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Utils;

public class MainMenu : MonoBehaviour
{ 
    [Header("PANEL MAIN MENU")]
    [SerializeField] private GameObject _mainMenu;
    [SerializeField] private Button _play;
    [SerializeField] private Button _options;
    [SerializeField] private Button _exit;
    
    [Header("PANEL LEVEL SELECTOR")]
    [SerializeField] private GameObject _lvlSelector;
    [SerializeField] private Button[] _lvlsButtons;
    [SerializeField] private Button _backToMain;
    
    [Header("PANEL CHARGE LEVEL")]
    [SerializeField] private GameObject _chargeLvlSelected;

    
    private void Awake()
    {
        //Buttons clicks//
        _play.onClick.AddListener(ShowLvlSelector);
        //_options.onClick.AddListener();
        _exit.onClick.AddListener(QuitGame);
        _backToMain.onClick.AddListener(ShowMain);

        SetLevelsInButtons();
    }

    private void Start()
    {
        CheckEnabledLevels();
    }

    private void CheckEnabledLevels()
    {
        int levelAt = PlayerPrefs.GetInt(LVL_AT, 1);

        for (int i = 0; i < _lvlsButtons.Length; i++)
        {
            if (i + LVL_FIRST > levelAt) //habilito solo el primer lvl
            {
                _lvlsButtons[i].interactable = false;
            }
        }
    }

    private void SetLevelsInButtons()
    {
        for (int i = 0; i < _lvlsButtons.Length; i++)
        {
            int levelIndex = i + LVL_FIRST; // El indice de la escena empieza desde 1 porque 0 es el MainMenu
            _lvlsButtons[i].onClick.AddListener(() => WhereGoLevelButtons(levelIndex));
        }
    }

    private void WhereGoLevelButtons(int levelIndex)
    {
        _chargeLvlSelected.SetActive(true);
        _mainMenu.SetActive(false);
        _lvlSelector.SetActive(false);

        StartCoroutine(LoadLevelAfterDelay(levelIndex));
    }
    
    private IEnumerator LoadLevelAfterDelay(int levelIndex)
    {
        yield return new WaitForSeconds(FAKE_LOADING_TIME_SCENE);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private void ShowMain()
    {
        _mainMenu.SetActive(true);
        _lvlSelector.SetActive(false);
    }

    private void ShowLvlSelector()
    {
        _mainMenu.SetActive(false);
        _lvlSelector.SetActive(true);
    }
    
    private void QuitGame()
    {
#if UNITY_EDITOR // Si estás en el editor, detiene la ejecución del juego
        UnityEditor.EditorApplication.isPlaying = false;
#else // En una compilación, cierra la aplicación
            Application.Quit();
#endif
    }

}
