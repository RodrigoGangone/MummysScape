
using System.Collections.Generic;
using UnityEngine;

public static class LevelManagerJson
{
    private const string LEVELS_KEY = "levels";
    private static List<Level> levels = new();
    public static List<Level> Levels => levels;
    
    public static void LoadLevels() //cargar niveles si lo hay
    {
        if (PlayerPrefs.HasKey(LEVELS_KEY))
        {
            string json = PlayerPrefs.GetString(LEVELS_KEY);
            levels = JsonUtility.FromJson<LevelList>(json).levels;
            Debug.Log($"LoadLevels\n{json}");
        }
    }


    public static void AddNewLevel(int levelNumber, List<CollectibleNumber> collectibles, float timeToComplete)
    {
        Level newLevel = new Level
        {
            level = levelNumber,
            collectibleNumbers = collectibles,
            timeToCompleteLevel = timeToComplete
        };

        ValidationDataOfSameLevel(newLevel);
        SaveLevels();
    }
    
    public static int GetLevelCount()
    {
        Debug.Log($"GetLevelCount = {levels.Count}");
        return levels.Count;
    }
    
    private static void SaveLevels()
    {
        string json = JsonUtility.ToJson(new LevelList { levels = levels }, true);
        PlayerPrefs.SetString(LEVELS_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("Levels saved to PlayerPrefs");
    }

    public static void DeteleLevels()
    {
        PlayerPrefs.DeleteKey(LEVELS_KEY);
        levels.Clear();
    }

    private static void ValidationDataOfSameLevel(Level newLevel)
    {
        Level existingLevel = levels.Find(level => level.level == newLevel.level);
    
        if (existingLevel != null)
        {
            foreach (var collectible in newLevel.collectibleNumbers)
            {
                if (!existingLevel.collectibleNumbers.Contains(collectible))
                {
                    existingLevel.collectibleNumbers.Add(collectible);
                }
            }

            if (newLevel.timeToCompleteLevel < existingLevel.timeToCompleteLevel)
            {
                existingLevel.timeToCompleteLevel = newLevel.timeToCompleteLevel;
            }

            Debug.Log($"Level {newLevel.level} updated with new collectibles and time.");
        }
        else
        {
            levels.Add(newLevel);
            Debug.Log($"New level {newLevel.level} added.");
        }
    }

    public static void SHOWPREFLEVELS()
    {
        string json = JsonUtility.ToJson(new LevelList { levels = levels }, true);
        Debug.Log($"LoadLevels\n{json}");
    }
}