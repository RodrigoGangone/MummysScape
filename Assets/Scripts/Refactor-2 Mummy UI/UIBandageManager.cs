using System;
using UnityEngine;

public class UIBandageManager : MonoBehaviour
{
    // Desde el Inspector, arrastra aquí tus dos objetos de la UI.
    [SerializeField]
    private BandageController bandageOneController; 

    [SerializeField]
    private BandageController bandageTwoController;
    
    
    
    //debug
    public int _currentBandages = 2;

    private void Start()
    {
        //debug 
        UpdateBandageCount(_currentBandages);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (_currentBandages > 0)
            {
                _currentBandages--;
                UpdateBandageCount(_currentBandages);
            }
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_currentBandages < 2)
            {
                _currentBandages++;
                UpdateBandageCount(_currentBandages);
            }
        }
    }

    /// <summary>
    /// El método principal para actualizar la UI.
    /// Llámalo desde tu script de Player cada vez que la cantidad de vendas cambie.
    /// </summary>
    /// <param name="currentBandageCount">La cantidad actual de vendas (0, 1, o 2).</param>
    public void UpdateBandageCount(int currentBandageCount)
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
}