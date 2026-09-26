using UnityEngine;

public class SelectorNavigation : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField]
    private SceneTransitionManager transitionManager;

    private bool _isLoading;

    public bool IsLoading =>
        _isLoading;

    // ============================================================
    // LEVEL
    // ============================================================

    public void EnterLevel(LevelTile level)
    {
        if (_isLoading)
            return;

        if (level == null)
        {
            Debug.LogWarning(
                "[SelectorNavigation] LevelTile nulo."
            );

            return;
        }

        if (!level.IsPlayable)
        {
            Debug.LogWarning(
                $"[SelectorNavigation] El nivel '{level.LevelId}' está bloqueado."
            );

            return;
        }

        if (!ValidateTransitionManager())
            return;

        _isLoading = true;

        Save.SetLastLevelPlayed(
            level.BuildIndex
        );

        transitionManager.FadeInAndLoadScene(
            level.BuildIndex
        );
    }

    // ============================================================
    // NEXT ZONE
    // ============================================================

    public void EnterNextZone(ZoneTile portal)
    {
        if (_isLoading)
            return;

        if (portal == null)
        {
            Debug.LogWarning(
                "[SelectorNavigation] ZoneTile nulo."
            );

            return;
        }

        if (!portal.IsUnlocked)
        {
            Debug.LogWarning(
                $"[SelectorNavigation] El Portal '{portal.ZoneId}' todavía está bloqueado."
            );

            return;
        }

        if (!ValidateTransitionManager())
            return;

        _isLoading = true;

        transitionManager.FadeInAndLoadScene(
            portal.TargetSelectorBuildIndex
        );
    }

    // ============================================================

    private bool ValidateTransitionManager()
    {
        if (transitionManager != null)
            return true;

        Debug.LogError(
            "[SelectorNavigation] Falta SceneTransitionManager."
        );

        return false;
    }
}