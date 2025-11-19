using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public class LevelTile : MonoBehaviour
{
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel;
    [SerializeField] private bool isBossLevel;
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];

    private FocusOnActivation FocusOnActivation => GetComponent<FocusOnActivation>();
    
    [Header("TRANSICION")] 
    
    [SerializeField] private SceneTransitionManager transition;

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

        if (!isBossLevel)
            SetAllGems(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = true;
        portalFx.Play();
        
        if (!isBossLevel)
            RefreshGems();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        
        portalFx.Stop();
        
        if (!isBossLevel)
            SetAllGems(false);
    }

    private void Update()
    {
        if (_playerInside && (Input.GetKeyDown(KeyCode.X) || Input.GetButtonDown("Space")))
        {
            if (transition != null)
                transition.FadeInAndLoadScene(buildIndex);
            
            FocusOnActivation.Activate();
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