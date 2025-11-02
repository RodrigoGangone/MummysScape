using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelTile : MonoBehaviour
{
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel = false; // <-- AÑADE ESTO
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];

    bool _playerInside;

    void Start()
    {
        // --- LÓGICA DE DESBLOQUEO MODIFICADA ---
        bool isUnlocked;
        
        if (isFirstLevel)
        {
            // El primer nivel siempre está desbloqueado
            isUnlocked = true;
        }
        else
        {
            // Para otros niveles, comprueba si el ANTERIOR (índice - 1) fue completado
            int previousLevelIndex = buildIndex - 1;
            isUnlocked = Save.IsLevelCompleted(previousLevelIndex);
        }
        // --- FIN DE LA MODIFICACIÓN ---

        if (!isUnlocked)
        {
            gameObject.SetActive(false);
            return;
        }

        // Si llegó hasta aquí, es porque está desbloqueado
        // Arranca sin mostrar gemas (se muestran al entrar)
        SetAllGems(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = true;
        RefreshGems();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        SetAllGems(false);
    }

    void Update()
    {
        if (_playerInside && Input.GetKeyDown(KeyCode.X))
            SceneManager.LoadScene(buildIndex);
    }

    void RefreshGems()
    {
        for (int i = 0; i < gemIcons.Length; i++)
        {
            var g = gemIcons[i];
            if (!g) continue;

            int gemNum = i + 1;
            // Usamos el 'buildIndex' de este tile para saber qué gemas mostrar
            bool picked = Save.WasGemPickedInLevel(gemNum, buildIndex);
            g.SetActive(picked);
        }
    }

    void SetAllGems(bool on)
    {
        for (int i = 0; i < gemIcons.Length; i++)
            if (gemIcons[i]) gemIcons[i].SetActive(on);
    }
}