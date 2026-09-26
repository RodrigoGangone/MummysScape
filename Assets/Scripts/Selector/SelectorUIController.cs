using TMPro;
using UnityEngine;

public class SelectorUIController : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField]
    private SelectorController selectorController;

    [Header("Level Panel")]
    [SerializeField]
    private GameObject levelPanel;

    [SerializeField]
    private TextMeshProUGUI levelNameText;

    [SerializeField]
    private TextMeshProUGUI levelStatusText;

    [SerializeField]
    private TextMeshProUGUI levelGemCountText;

    [Header("Level Gem Slots")]
    [SerializeField]
    private GameObject[] collectedGemVisuals;

    [SerializeField]
    private GameObject[] missingGemVisuals;

    [Header("Portal Panel")]
    [SerializeField]
    private GameObject portalPanel;

    [SerializeField]
    private TextMeshProUGUI portalNameText;

    [SerializeField]
    private TextMeshProUGUI portalStatusText;

    [SerializeField]
    private TextMeshProUGUI portalBossRequirementText;

    [SerializeField]
    private TextMeshProUGUI portalGemRequirementText;

    private void OnEnable()
    {
        if (selectorController != null)
        {
            selectorController.OnSelectionChanged +=
                Refresh;
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (selectorController != null)
        {
            selectorController.OnSelectionChanged -=
                Refresh;
        }
    }

    public void Refresh()
    {
        if (selectorController == null)
        {
            HideAll();
            return;
        }

        if (selectorController.IsFinalPortalSelected)
        {
            ShowPortal(
                selectorController.CurrentPortal
            );

            return;
        }

        ShowLevel(
            selectorController.CurrentLevel
        );
    }

    // ============================================================
    // LEVEL
    // ============================================================

    private void ShowLevel(LevelTile level)
    {
        if (portalPanel != null)
            portalPanel.SetActive(false);

        if (levelPanel != null)
            levelPanel.SetActive(true);

        if (level == null)
            return;

        LevelTileState state =
            level.State;

        if (levelNameText != null)
            levelNameText.text =
                level.DisplayName;

        if (levelStatusText != null)
        {
            levelStatusText.text =
                ResolveLevelStatus(
                    state.Availability
                );
        }

        if (levelGemCountText != null)
        {
            int total =
                state.Gems != null
                    ? state.Gems.Length
                    : 0;

            levelGemCountText.text =
                $"{state.CollectedGemCount}/{total}";
        }

        RefreshLevelGems(
            state.Gems
        );
    }

    private string ResolveLevelStatus(
        LevelAvailability availability)
    {
        switch (availability)
        {
            case LevelAvailability.Completed:
                return "COMPLETADO";

            case LevelAvailability.Available:
                return "DISPONIBLE";

            case LevelAvailability.Locked:
            default:
                return "BLOQUEADO";
        }
    }

    private void RefreshLevelGems(
        bool[] gems)
    {
        int slotCount =
            Mathf.Max(
                collectedGemVisuals != null
                    ? collectedGemVisuals.Length
                    : 0,

                missingGemVisuals != null
                    ? missingGemVisuals.Length
                    : 0
            );

        for (int i = 0;
             i < slotCount;
             i++)
        {
            bool collected =
                gems != null &&
                i < gems.Length &&
                gems[i];

            if (collectedGemVisuals != null &&
                i < collectedGemVisuals.Length &&
                collectedGemVisuals[i] != null)
            {
                collectedGemVisuals[i]
                    .SetActive(collected);
            }

            if (missingGemVisuals != null &&
                i < missingGemVisuals.Length &&
                missingGemVisuals[i] != null)
            {
                missingGemVisuals[i]
                    .SetActive(!collected);
            }
        }
    }

    // ============================================================
    // FINAL PORTAL
    // ============================================================

    private void ShowPortal(
        ZoneTile portal)
    {
        if (levelPanel != null)
            levelPanel.SetActive(false);

        if (portalPanel != null)
            portalPanel.SetActive(true);

        if (portal == null)
            return;

        ZoneTileState state =
            portal.State;

        if (portalNameText != null)
        {
            portalNameText.text =
                portal.DisplayName;
        }

        if (portalStatusText != null)
        {
            portalStatusText.text =
                state.IsUnlocked
                    ? "PORTAL DISPONIBLE"
                    : "PORTAL BLOQUEADO";
        }

        if (portalBossRequirementText != null)
        {
            portalBossRequirementText.text =
                state.BossCompleted
                    ? "BOSS: COMPLETADO"
                    : "BOSS: PENDIENTE";
        }

        if (portalGemRequirementText != null)
        {
            portalGemRequirementText.text =
                $"GEMAS: {state.CurrentGems}/{state.RequiredGems}";
        }
    }

    // ============================================================

    private void HideAll()
    {
        if (levelPanel != null)
            levelPanel.SetActive(false);

        if (portalPanel != null)
            portalPanel.SetActive(false);
    }
}