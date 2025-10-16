using System;
using UnityEngine;

public class UIBandageManager : MonoBehaviour
{
    // Desde el Inspector, arrastra aquí tus dos objetos de la UI.
    [SerializeField] private BandageController bandageOneController;

    [SerializeField] private BandageController bandageTwoController;
    
    private void UpdateBandageCount(int currentBandageCount)
    {
        // Usamos un switch para que la lógica sea súper clara.
        switch (currentBandageCount)
        {
            case 0:
                // No tiene vendas, ocultamos ambas.
                bandageOneController.showBandage = false;
                bandageTwoController.showBandage = false;
                break;

            case 1:
                // Tiene 1 venda, mostramos solo la segunda.
                // (Según tu lógica: 2->1 oculta la primera).
                bandageOneController.showBandage = false;
                bandageTwoController.showBandage = true;
                break;

            case 2:
                // Tiene 2 vendas, mostramos ambas.
                bandageOneController.showBandage = true;
                bandageTwoController.showBandage = true;
                break;
        }
    }

    // --- Métodos de prueba para usar desde el Inspector ---
    [ContextMenu("Test: Set Count to 0")]
    private void TestSetCountToZero() => UpdateBandageCount(0);

    [ContextMenu("Test: Set Count to 1")]
    private void TestSetCountToOne() => UpdateBandageCount(1);

    [ContextMenu("Test: Set Count to 2")]
    private void TestSetCountToTwo() => UpdateBandageCount(2);
    
    private void OnEnable() => GameEventManager.Instance.playerEvents.OnBandageCount.Register<int>(UpdateBandageCount);
    private void OnDisable() => GameEventManager.Instance.playerEvents.OnBandageCount.Unregister<int>(UpdateBandageCount);

}