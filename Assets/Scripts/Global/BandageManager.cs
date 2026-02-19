using UnityEngine;
using System.Collections.Generic;

public class BandageManager : MonoBehaviour
{
    [SerializeField] private int maxBandagesInScene = 5;

    private List<GameObject> activeBandages = new List<GameObject>();
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnRequestBandageSpawn.Register<GameObject, bool>(HandleBandageUpdate);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnRequestBandageSpawn.Unregister<GameObject, bool>(HandleBandageUpdate);

    private void HandleBandageUpdate(GameObject bandage, bool isActive)
    {
        if (isActive)
        {
            // Lógica FIFO: Si está lleno, apagamos la más vieja
            if (activeBandages.Count >= maxBandagesInScene)
            {
                // Al hacer esto, la venda llamará a este mismo método con 'isActive = false'
                activeBandages[0].SetActive(false);
            }

            if (!activeBandages.Contains(bandage))
            {
                activeBandages.Add(bandage);
            }
        }
        else
        {
            // Si la venda se apaga, la removemos de la lista
            if (activeBandages.Contains(bandage))
            {
                activeBandages.Remove(bandage);
            }
        }
    }
}