using System;
using UnityEngine;

public class UIBandageManager : MonoBehaviour
{
    [Header("Controladores de Vendas")]
    [SerializeField] private BandageController bandageOneController;
    [SerializeField] private BandageController bandageTwoController;
    
    [Header("Controlador del Reloj de Arena")]
    [SerializeField] private HourglassController hourglassController;

    private int _previousBandageCount = -1;// <-- LÍNEA NUEVA (para detectar cambios)
    //debug
    public int _currentBandages = 2;

    private void OnDisable()
    {
        GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Unregister<int>(UpdateBandageCount);
    }

    private void Awake()
    {
        // forzamos el reloj al estado inicial antes del primer frame >>>
        hourglassController.SnapByBandageCount(_currentBandages);
        // También seteamos el estado visual de las vendas para arrancar consistente.
        _previousBandageCount = _currentBandages; // evita transiciones al primer UpdateBandageCount
    }

    private void Start()
    {
        GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Register<int>(UpdateBandageCount);
        GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Raise(_currentBandages);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (_currentBandages > 0)
            {
                _currentBandages--;
                GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Raise(_currentBandages);
                Debug.Log("++");
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_currentBandages < 2)
            {
                _currentBandages++;
                GameEventManager.Instance.playerEvents.OnBandagesCountChanged.Raise(_currentBandages);
                Debug.Log("--");
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
        if (currentBandageCount == 0 && _previousBandageCount > 0)
        {
            hourglassController.StartCountdown();
        }
        // Si acabamos de obtener una venda (saliendo de 0), reinicia el reloj.
        else if (currentBandageCount > 0 && _previousBandageCount == 0)
        {
            hourglassController.ResetAndFill();
        }
        
        // Al final, actualizamos el contador previo.
        _previousBandageCount = currentBandageCount;
        
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