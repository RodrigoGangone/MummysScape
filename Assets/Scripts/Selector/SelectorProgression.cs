using UnityEngine;

public class SelectorProgression : MonoBehaviour
{
    [Header("Gems")]
    [SerializeField]
    [Min(1)]
    private int gemsPerLevel = 3;

    [Header("Reveal")]
    [SerializeField]
    private bool evaluateReveals = false;

    [Header("Debug - Levels")]
    [SerializeField]
    private bool unlockAllLevelsForDebug = false;

    [Header("Debug - Final Portal")]
    [Tooltip("Permite probar el Portal como si el Boss estuviera completado.")]
    [SerializeField]
    private bool forceBossCompletedForDebug = false;

    [Tooltip("Permite probar el Portal como si ya hubiera suficientes gemas.")]
    [SerializeField]
    private bool forceEnoughGemsForDebug = false;

    // ============================================================
    // LEVELS
    // ============================================================

    public LevelTileState EvaluateLevel(
        LevelTile level,
        LevelTile previousLevel)
    {
        if (level == null)
            return default;

        bool completed =
            Save.IsLevelCompleted(
                level.BuildIndex
            );

        bool unlocked;

        if (unlockAllLevelsForDebug)
        {
            unlocked = true;
        }
        else if (previousLevel == null)
        {
            unlocked = true;
        }
        else
        {
            unlocked =
                Save.IsLevelCompleted(
                    previousLevel.BuildIndex
                );
        }

        LevelAvailability availability;

        if (completed)
        {
            availability =
                LevelAvailability.Completed;
        }
        else if (unlocked)
        {
            availability =
                LevelAvailability.Available;
        }
        else
        {
            availability =
                LevelAvailability.Locked;
        }

        bool[] gems =
            GetLevelGems(level);

        bool revealPending =
            evaluateReveals &&
            unlocked &&
            !Save.IsLevelRevealSeen(
                level.BuildIndex
            );

        return new LevelTileState(
            availability,
            gems,
            revealPending
        );
    }

    public bool[] GetLevelGems(
        LevelTile level)
    {
        bool[] gems =
            new bool[gemsPerLevel];

        if (level == null)
            return gems;

        for (int i = 0; i < gems.Length; i++)
        {
            gems[i] =
                Save.WasGemPickedInLevel(
                    i + 1,
                    level.BuildIndex
                );
        }

        return gems;
    }

    // ============================================================
    // FINAL PORTAL
    // ============================================================

    public ZoneTileState EvaluateFinalPortal(
        ZoneTile portal,
        LevelTile bossLevel)
    {
        if (portal == null ||
            bossLevel == null)
        {
            return default;
        }

        bool bossCompleted =
            forceBossCompletedForDebug ||
            Save.IsLevelCompleted(
                bossLevel.BuildIndex
            );

        int currentGems =
            Save.GetGlobalGemCount();

        if (forceEnoughGemsForDebug)
        {
            currentGems =
                Mathf.Max(
                    currentGems,
                    portal.RequiredGems
                );
        }

        bool requirementsMet =
            bossCompleted &&
            currentGems >= portal.RequiredGems;

        bool revealPending =
            evaluateReveals &&
            requirementsMet &&
            !Save.IsZoneRevealSeen(
                portal.TargetSelectorBuildIndex
            );

        return new ZoneTileState(
            bossCompleted,
            currentGems,
            portal.RequiredGems,
            revealPending
        );
    }
}