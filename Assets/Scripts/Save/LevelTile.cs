using System;
using UnityEngine;
using UnityEngine.Serialization;
using static buttonType;

[RequireComponent(typeof(Collider))]
public class LevelTile : MonoBehaviour
{
    [SerializeField] private int buildIndex;
    [SerializeField] private bool isFirstLevel;
    [SerializeField] private bool isBossLevel;
    [SerializeField] private ParticleSystem portalFx;
    [SerializeField] private GameObject[] gemIcons = new GameObject[3];

    [Header("CONFIGURACIÓN BLOQUEO")] 
    [SerializeField] private Material lockedMaterial;
    
    private FocusOnActivation FocusOnActivation => GetComponent<FocusOnActivation>();

    [Header("TRANSICION")] [SerializeField]
    private UIManager uiManager;

    bool _playerInside;
    bool _isUnlocked;

    void Start()
    {
        if (isFirstLevel)
            _isUnlocked = true;
        else
        {
            int previousLevelIndex = buildIndex - 1;
            _isUnlocked = Save.IsLevelCompleted(previousLevelIndex);
        }

        if (!_isUnlocked)
        {
            ApplyLockedMaterial();
            return;
        }

        if (!isBossLevel)
            SetAllGems(false);
        
        if(uiManager != null)
            GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(false, A);
    }

    private void ApplyLockedMaterial()
    {
        if (lockedMaterial == null)
        {
            Debug.LogWarning("No has asignado el material gris en el inspector de LevelTile.");
            return;
        }

        // Buscamos todos los renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            // Creamos un array del mismo tamaño que los materiales actuales del objeto
            // por si el objeto tiene más de un slot de material
            Material[] newMaterials = new Material[rend.sharedMaterials.Length];

            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = lockedMaterial;
            }

            // Asignamos el nuevo array de materiales
            rend.materials = newMaterials;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isUnlocked || !other.CompareTag("PlayerFather")) return;

        _playerInside = true;

        if (portalFx != null) portalFx.Play();

        if (!isBossLevel)
            RefreshGems();

        GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(true, A);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isUnlocked || !other.CompareTag("PlayerFather")) return;
        _playerInside = false;
        if (portalFx != null) portalFx.Stop();

        if (!isBossLevel)
            SetAllGems(false);

        GameEventManager.Instance.levelEvents.OnTutorialPrompt.Raise(false, A);
    }

    private void Update()
    {
        if (_isUnlocked && _playerInside && (Input.GetKeyDown(KeyCode.X) || Input.GetButtonDown("Space")))
        {
            uiManager.GetComponent<SceneTransitionManager>().FadeInAndLoadScene(buildIndex);

            if (FocusOnActivation != null) FocusOnActivation.Activate();
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