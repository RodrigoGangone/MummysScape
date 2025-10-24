using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelTile : MonoBehaviour
{
    [SerializeField] private int buildIndex;
    [SerializeField] private GameObject[] gemIcons = new GameObject[3]; // orden 1,2,3
    [SerializeField] private string playerTag = "PlayerFather";
    [SerializeField] private KeyCode activateKey = KeyCode.X;

    bool _playerInside;

    void Start()
    {
        // Si no está desbloqueado, apago el cubo y listo
        if (!Save.IsLevelUnlockedByIndex(buildIndex))
        {
            gameObject.SetActive(false);
            return;
        }

        // Arranca sin mostrar gemas (se muestran al entrar)
        SetAllGems(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = true;
        RefreshGems(); // muestra el estado real de las gemas 1..3 en este nivel
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
        SetAllGems(false); // al salir, oculto las gemas
    }

    void Update()
    {
        if (_playerInside && Input.GetKeyDown(activateKey))
            SceneManager.LoadScene(buildIndex);
    }

    void RefreshGems()
    {
        for (int i = 0; i < gemIcons.Length; i++)
        {
            var g = gemIcons[i];
            if (!g) continue;

            int gemNum = i + 1;
            bool picked = Save.WasGemPickedInLevel(gemNum, buildIndex);
            g.SetActive(picked); // ON si esa gema fue obtenida, OFF si no
        }
    }

    void SetAllGems(bool on)
    {
        for (int i = 0; i < gemIcons.Length; i++)
            if (gemIcons[i]) gemIcons[i].SetActive(on);
    }
}