using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Slider _lifetime;
    [SerializeField] Slider _shootSlider;
    private Player _player;
    int previousVandage = 0;

    void Start()
    {
        _player = FindObjectOfType<Player>();
        _player._modelPlayer.sizeModify += UISetShootSlider;
        StartCoroutine(SetValue(1, _player.CurrentBandageStock));
    }

    public void UISetTimerDeath(float currentTimer, float maxtime)
    {
        _lifetime.value = Mathf.Clamp01(currentTimer / maxtime);
    }
    public void UISetShootSlider()
    {
        StartCoroutine(SetValue(1, _player.CurrentBandageStock));
    }

    IEnumerator SetValue(float time, int currentVandage)
    {
        
        int starlerp = previousVandage;
        int endlerp = currentVandage;

        float tick = 0f;
        float value = starlerp;

        while (value != endlerp)
        {
            value = Mathf.Lerp(starlerp, endlerp, tick);

            _shootSlider.value = Mathf.Clamp01(value / 2);
            tick += Time.deltaTime / time;
            yield return null;
        }
        previousVandage = endlerp;
    }
}