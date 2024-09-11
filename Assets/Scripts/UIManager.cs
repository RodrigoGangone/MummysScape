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
    
    private float targetOffset1;
    private float targetOffset2;
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
        UpdateMaterialOffsets();
    }

    public void UISetTimerDeath(float currentTimer, float maxtime)
    {
        _lifetime.value = Mathf.Clamp01(currentTimer / maxtime);
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
        _HourgalssBandage01.SetFloat("_Offset", Mathf.MoveTowards(_HourgalssBandage01.GetFloat("_Offset"), targetOffset1, fillSpeed * Time.deltaTime));
        _HourgalssBandage02.SetFloat("_Offset", Mathf.MoveTowards(_HourgalssBandage02.GetFloat("_Offset"), targetOffset2, fillSpeed * Time.deltaTime));
    }
    
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