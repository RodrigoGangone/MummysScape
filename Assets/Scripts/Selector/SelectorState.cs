using System;

public enum LevelAvailability
{
    Locked,
    Available,
    Completed
}

public enum SelectorMode
{
    Initializing,
    Navigating,
    Moving,
    Revealing,
    Entering
}

[Serializable]
public struct LevelTileState
{
    public LevelAvailability Availability;
    public bool[] Gems;
    public bool RevealPending;

    public bool IsPlayable => Availability != LevelAvailability.Locked;

    public int CollectedGemCount
    {
        get
        {
            if (Gems == null)
                return 0;

            int count = 0;

            for (int i = 0; i < Gems.Length; i++)
            {
                if (Gems[i])
                    count++;
            }

            return count;
        }
    }

    public LevelTileState(
        LevelAvailability availability,
        bool[] gems,
        bool revealPending)
    {
        Availability = availability;
        Gems = gems;
        RevealPending = revealPending;
    }
}

[Serializable]
public struct ZoneTileState
{
    public bool BossCompleted;
    public int CurrentGems;
    public int RequiredGems;
    public bool RevealPending;

    public bool IsUnlocked =>
        BossCompleted &&
        CurrentGems >= RequiredGems;

    public ZoneTileState(
        bool bossCompleted,
        int currentGems,
        int requiredGems,
        bool revealPending)
    {
        BossCompleted = bossCompleted;
        CurrentGems = currentGems;
        RequiredGems = requiredGems;
        RevealPending = revealPending;
    }
}