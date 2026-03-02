using UnityEngine;

/// <summary> 
/// Coordinador de Vendas UI: Escucha los cambios en el inventario del jugador para activar o 
/// desactivar secuencialmente los controladores de visualización de los iconos de vendas. 
/// </summary>

public sealed class UIBandageManager : MonoBehaviour
{
    [Header("Controladores de Vendas")]
    [SerializeField] private BandageController bandageOneController;
    [SerializeField] private BandageController bandageTwoController;

    private void OnEnable()
    {
        var evt = GameEventManager.Instance.playerEvents.OnBandagesCountChanged;
        if (evt != null) evt.Register<int>(UpdateBandageCount);
    }

    private void OnDisable()
    {
        var evt = GameEventManager.Instance.playerEvents.OnBandagesCountChanged;
        if (evt != null) evt.Unregister<int>(UpdateBandageCount);
    }

    private void UpdateBandageCount(int currentBandageCount)
    {
        switch (currentBandageCount)
        {
            case 0:
                bandageOneController.showBandage = false;
                bandageTwoController.showBandage = false;
                break;
            case 1:
                bandageOneController.showBandage = false;
                bandageTwoController.showBandage = true;
                break;
            default: // 2 o más
                bandageOneController.showBandage = true;
                bandageTwoController.showBandage = true;
                break;
        }
    }
}