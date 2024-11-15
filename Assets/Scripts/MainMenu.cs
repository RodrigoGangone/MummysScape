using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Utils;

public class MainMenu : MonoBehaviour
{
    [Header("PANEL MAIN MENU")] [SerializeField]
    private GameObject _mainMenuPanel;

    [SerializeField] private Button _btnPlay;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnExit;

    [Header("PANEL OPTIONS")] [SerializeField]
    private GameObject _optionsPanel;

    [SerializeField] private Button _btnDeletePrefs;
    [SerializeField] private TMP_Dropdown _frameRateSpinner;

    private static List<string> FrameRateText => new(FPS.Keys);

    [Header("PANEL LEVEL SELECTOR")] [SerializeField]
    private GameObject _lvlSelectorPanel;

    [SerializeField] private Button[] _btnsLvls;

    [Header("PANEL CHARGE LEVEL")] [SerializeField]
    private GameObject _chargeLvlSelected;

    [SerializeField] private Button _btnBackToMain;

    private DepthOfField _blur;
    private Volume _postProcess;

    private void Awake()
    {
        //Buttons Main//
        AddButtonProps(_btnPlay, ShowLvlSelector);
        AddButtonProps(_btnOptions, ShowOptions);
        AddButtonProps(_btnExit, QuitGame);
        
        //Buttons Options//
        _frameRateSpinner.AddOptions(FrameRateText);
        _frameRateSpinner.onValueChanged.AddListener(delegate { OnDropdownValueChanged(_frameRateSpinner); });
        
        AddButtonProps(_btnDeletePrefs, LevelManagerJson.DeteleLevels, CheckEnabledLevels);
        AddButtonProps(_btnBackToMain, ShowMain);

        SetLevelsInButtons();
    }
    
    private void AddButtonProps(Button button, Action mainAction, params Action[] additionalActions)
    {
        button.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlaySFX(NameSounds.Click);
        
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

    private void Start()
    {
        //Activar Blur en la scene
        _postProcess = FindObjectOfType<Volume>();

        if (_postProcess.profile.TryGet(out _blur))
            _blur.active = !_blur.active;

        //Check niveles
        CheckEnabledLevels();

        //Check Options [FPS]
        CheckOptions();
    }
    
    private void CheckOptions() //TODO: MODIFICAR ESTO PARA QUE EXISTA UN JSON QUE GUARDE TODAS LAS OPTIONS
    {
        _frameRateSpinner.value =
            _frameRateSpinner.options.FindIndex(option =>
                option.text == PlayerPrefs.GetString(SELECTED_FPS_KEY, "60 FPS"));
    }

    private void CheckEnabledLevels()
    {
        LevelManagerJson.LoadLevels(); //verifico los niveles
        int levelAt = LevelManagerJson.GetLevelCount(); //obtengo la cantidad de niveles del json

        for (int i = 0; i < _btnsLvls.Length; i++)
        {
            if (i <= levelAt)
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

    public void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        string selectedFPSKey = dropdown.options[dropdown.value].text;

        Application.targetFrameRate = FPS[selectedFPSKey];

        Debug.Log("FPS SELECCIONADO " + selectedFPSKey);
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