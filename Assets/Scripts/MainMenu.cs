using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Utils;

public class MainMenu : MonoBehaviour
{ 
    [Header("PANEL MAIN MENU")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private Button _btnPlay;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;
    
    [Header("PANEL OPTIONS")]
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Button _btnDeletePrefs;
    
    [Header("PANEL LEVEL SELECTOR")]
    [SerializeField] private GameObject _lvlSelectorPanel;
    [SerializeField] private Button[] _btnsLvls;
    
    [Header("PANEL CHARGE LEVEL")]
    [SerializeField] private GameObject _chargeLvlSelected;

    [SerializeField] private Button _btnBackToMain;
    
    private void Awake()
    {
        //Buttons Main//
        _btnPlay.onClick.AddListener(ShowLvlSelector);
        _btnOptions.onClick.AddListener(ShowOptions);
        _btnExit.onClick.AddListener(QuitGame);
        
        //Buttons Options//
        _btnDeletePrefs.onClick.AddListener(()=>
        {
            LevelManagerJson.DeteleLevels();
            CheckEnabledLevels();
        });
        
        _btnBackToMain.onClick.AddListener(ShowMain);

        SetLevelsInButtons();
    }

    private void Start()
    {
        CheckEnabledLevels();
    }

    private void CheckEnabledLevels()
    {
        LevelManagerJson.LoadLevels(); //verifico los niveles
        int levelAt = LevelManagerJson.GetLevelCount(); //obtengo la cantidad de niveles del json
        
        for (int i = 0; i < _btnsLvls.Length; i++)
        {
            if (i  <= levelAt) 
            {
                _btnsLvls[i].interactable = true;  
            }
            else
            {
                _btnsLvls[i].interactable = false; 
            }
        }
    }

    private void SetLevelsInButtons()
    {
        for (int i = 0; i < _btnsLvls.Length; i++)
        {
            int levelIndex = i + LEVEL_FIRST; // El indice de la escena empieza desde 1 porque 0 es el MainMenu
            _btnsLvls[i].onClick.AddListener(() => WhereGoLevelButtons(levelIndex));
        }
    }

    private void WhereGoLevelButtons(int levelIndex)
    {
        _chargeLvlSelected.SetActive(true);
        _mainMenuPanel.SetActive(false);
        _lvlSelectorPanel.SetActive(false);

        _btnBackToMain.gameObject.SetActive(false);
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
        _mainMenuPanel.SetActive(true);
        _optionsPanel.SetActive(false);
        _lvlSelectorPanel.SetActive(false);

        _btnBackToMain.gameObject.SetActive(false);
    }

    private void ShowOptions()
    {
        _mainMenuPanel.SetActive(false);
        _optionsPanel.SetActive(true);
        _lvlSelectorPanel.SetActive(false);
        
        _btnBackToMain.gameObject.SetActive(true);
    }

    private void ShowLvlSelector()
    {
        _mainMenuPanel.SetActive(false);
        _optionsPanel.SetActive(false);
        _lvlSelectorPanel.SetActive(true);
        
        _btnBackToMain.gameObject.SetActive(true);
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
