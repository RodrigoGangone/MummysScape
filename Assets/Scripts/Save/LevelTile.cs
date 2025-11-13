using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelTile : MonoBehaviour
{
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel = false;
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];

    [Header("TRANSICION")] [SerializeField] private SceneTransitionManager _transition;

    bool _playerInside;

    void Start()
    {
        bool isUnlocked;

        if (isFirstLevel)
            isUnlocked = true;
        else
        {
            int previousLevelIndex = buildIndex - 1;
            isUnlocked = Save.IsLevelCompleted(previousLevelIndex);
        }
    
        if (!isUnlocked)
        {
            gameObject.SetActive(false);
            return;
        }

        SetAllGems(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = true;
        RefreshGems();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        SetAllGems(false);
    }

    private void Update()
    {
        if (_playerInside && Input.GetKeyDown(KeyCode.X))
        {
            if (_transition != null)
                _transition.FadeInAndLoadScene(buildIndex);
        }
    }

    private void RefreshGems()
    {
        for (int i = 0; i < gemIcons.Length; i++)
        {
            var g = gemIcons[i];
            if (!g) continue;

            int gemNum = i + 1;
            bool picked = Save.WasGemPickedInLevel(gemNum, buildIndex);
            g.SetActive(picked);
        }
    }

    private void SetAllGems(bool on)
    {
        foreach (var g in gemIcons)
            if (g)
                g.SetActive(on);
    }
}