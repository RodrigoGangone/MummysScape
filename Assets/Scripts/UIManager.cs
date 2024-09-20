using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private Player _player;
    private LevelManager levelManager;

    [SerializeField] private Image fadeImage;
    
    [SerializeField] Image _beetleCount;

    [SerializeField] private Material _HourgalssBandage01;
    [SerializeField] private Material _HourgalssBandage02;


    [SerializeField] private Material _sandTimer01;
    [SerializeField] private Material _sandTimer02;

    private float targetOffset1;
    private float targetOffset2;

    private float targetOffset3;

    private float fillSpeed = 1f;

    void Start()
    {
        _player = FindObjectOfType<Player>();
        _player._modelPlayer.SizeModify += UISetShootSlider;

        levelManager = FindObjectOfType<LevelManager>();
        levelManager.OnPlayerWin += Win;
        levelManager.OnPlayerDeath += Lose;

        UpdateTargetOffsets(); // Inicializar valores correctos
    }

    private void Update()
    {
        UISetTimerDeath(levelManager._currentTimeDeath, levelManager._maxTimeDeath);
        UpdateMaterialOffsets();
    }

    public void UISetTimerDeath(float currentTimer, float maxtime)
    {
        targetOffset3 = Mathf.Clamp01(currentTimer / maxtime);
    }

    public void UISetShootSlider()
    {
        UpdateTargetOffsets(); // Actualizar valores de offset según las vendas actuales
    }

    private void UpdateTargetOffsets()
    {
        int currentBandage = _player.CurrentBandageStock;

        targetOffset1 = currentBandage;
        targetOffset2 = currentBandage;
    }

    private void UpdateMaterialOffsets()
    {
        _HourgalssBandage01.SetFloat("_Offset",
            Mathf.MoveTowards(_HourgalssBandage01.GetFloat("_Offset"), targetOffset1, fillSpeed * Time.deltaTime));
        _HourgalssBandage02.SetFloat("_Offset",
            Mathf.MoveTowards(_HourgalssBandage02.GetFloat("_Offset"), targetOffset2, fillSpeed * Time.deltaTime));

        _sandTimer01.SetFloat("_Fill",
            Mathf.MoveTowards(_sandTimer01.GetFloat("_Fill"), targetOffset3, fillSpeed * Time.deltaTime));
        _sandTimer02.SetFloat("_Fill",
            Mathf.MoveTowards(_sandTimer02.GetFloat("_Fill"), targetOffset3, fillSpeed * Time.deltaTime));
    }

    public void UISetCollectibleCount(int count)
    {
        Debug.Log("AGARRASTE UN COLECCIONABLE");
    }

    private void Win()
    {
        Debug.Log("Ganaste el nivel - UIManager");
        StartCoroutine(FadeIn());
    }

    private void Lose()
    {
        Debug.Log("Perdiste el nivel - UIManager");
        StartCoroutine(FadeIn());
    }
    
    private IEnumerator FadeIn()
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

        // Asegurarse de que el alpha llegue a 1
        fadeImage.color = new Color(color.r, color.g, color.b, 1f);
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

        // Asegurarse de que el alpha llegue a 0
        fadeImage.color = new Color(color.r, color.g, color.b, 0f);
    }

    private void OnEnable()
    {
        StartCoroutine(FadeOut());
    }
}