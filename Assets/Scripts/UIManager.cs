using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private Player _player;
    private LevelManager levelManager;
    
    [SerializeField] Slider _lifetime;
    [SerializeField] Slider _shootSlider;
    [SerializeField] Image _beetleCount;
    
    [SerializeField] private Material _HourgalssBandage01;
    [SerializeField] private Material _HourgalssBandage02;

    Coroutine _currentCoroutine;
    
    int previousBandage = 0;

    void Start()
    {
        _player = FindObjectOfType<Player>();
        _player._modelPlayer.SizeModify += UISetShootSlider;

        levelManager = FindObjectOfType<LevelManager>();
        levelManager.OnPlayerWin += Win;
        levelManager.OnPlayerDeath += Lose;

        StartCoroutine(SetValue(1, _player.CurrentBandageStock));
    }

    public void UISetTimerDeath(float currentTimer, float maxtime)
    {
        _lifetime.value = Mathf.Clamp01(currentTimer / maxtime);
    }

    public void UISetShootSlider()
    {
        if (_currentCoroutine != null)
        {
            StopAllCoroutines();
        }
        _currentCoroutine = StartCoroutine(SetValue(1, _player.CurrentBandageStock));
    }

    IEnumerator SetValue(float time, int currentVandage)
    {
        int starlerp = previousBandage;
        int endlerp = currentVandage;

        float tick = 0f;
        float value = starlerp;

        while (value != endlerp)
        {
            value = Mathf.Lerp(starlerp, endlerp, tick);

            _HourgalssBandage01.SetFloat("_Offset", value);
            _HourgalssBandage02.SetFloat("_Offset", value);
            tick += Time.deltaTime / time;
            yield return null;
        }

        //if (endlerp != 0)
        //    SetMaterialUI(0);

        previousBandage = endlerp;
    }

    //public void SetMaterialUI(float vel)
    //{
    //    _UIMaterialFill.SetFloat("_Velocity", _player.CurrentBandageStock == 0 ? Mathf.Lerp(0, 50, vel * 0.05f) : 0);
    //    _UIMaterialHandler.SetFloat("_Velocity", _player.CurrentBandageStock == 0 ? Mathf.Lerp(0, 50, vel * 0.05f) : 0);
    //}

    public void UISetCollectibleCount(int count)
    {
        switch (count)
        {
            case 0:
                _beetleCount.fillAmount = 0;
                break;
            case 1:
                _beetleCount.fillAmount = 0.25f;
                break;
            case 2:
                _beetleCount.fillAmount = 0.50f;
                break;
            case 3:
                _beetleCount.fillAmount = 0.75f;
                break;
        }
    }
    
    private void Win()
    {
        Debug.Log("Ganaste el nivel - UIManager");
        //TODO: Aca va el visual de la UI
    }
    private void Lose()
    {
        Debug.Log("Perdiste el nivel - UIManager");
        //TODO: Aca va el visual de la UI
    }
}