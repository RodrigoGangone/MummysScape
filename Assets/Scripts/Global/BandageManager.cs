using UnityEngine;
using System.Collections.Generic;

/// <summary> 
/// Controlador de Bandage: Administra la cantidad máxima de vendas activas en la escena utilizando 
/// una lógica FIFO (First-In, First-Out) para desactivar los ítems más antiguos y optimizar el rendimiento. 
/// </summary>
public class BandageManager : MonoBehaviour
{
    [SerializeField] private int maxBandagesInScene = 20;

    private List<GameObject> activeBandages = new();

    private void OnEnable() =>
        GameEventManager.Instance.levelEvents.OnRequestBandageSpawn.Register<GameObject, bool>(HandleBandageUpdate);

    private void OnDisable() =>
        GameEventManager.Instance.levelEvents.OnRequestBandageSpawn.Unregister<GameObject, bool>(HandleBandageUpdate);

    public void OnEmpoweredCompleted() => GameEventManager.Instance.playerEvents.OnEmpoweredCompleted.Raise();

    private void HandleBandageUpdate(GameObject bandage, bool isActive)
    {
        if (isActive)
        {
            if (activeBandages.Count >= maxBandagesInScene)
            {
                GameObject oldestBandage = activeBandages[0];
                activeBandages.RemoveAt(0);
                oldestBandage.SetActive(false); 
            }

            if (!activeBandages.Contains(bandage))
            {
                activeBandages.Add(bandage);
            }
        }
        else
        {
            if (activeBandages.Contains(bandage))
            {
                activeBandages.Remove(bandage);
            }
        }
    }
}