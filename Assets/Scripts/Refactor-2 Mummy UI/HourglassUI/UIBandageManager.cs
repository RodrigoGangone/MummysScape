using UnityEngine;

/// <summary>
/// UIBandageManager
/// Muestra/oculta los íconos de vendas según OnBandagesCountChanged.
/// No controla el reloj de arena (esa lógica vive en HourglassManager).
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