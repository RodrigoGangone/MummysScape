using UnityEngine;
using static Utils;

public static class PlayerPrefsHandler
{

    public static void SaveLevelAt(int level)
    {
        PlayerPrefs.SetInt(LEVEL_AT, level);
        PlayerPrefs.Save();
    }

    public static int GetLevelAt()
    {
        return PlayerPrefs.GetInt(LEVEL_AT, LEVEL_FIRST);
    }
    
    public static void ResetLevelAt()
    {
        PlayerPrefs.DeleteKey(LEVEL_AT);
    }
    
    
}
