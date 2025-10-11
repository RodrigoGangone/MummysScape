using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SaveSystem/PlayerPrefs Registry", fileName = "PlayerPrefsRegistry")]
public class PlayerPrefsRegistry : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string Key;
        public string Value;
        public string Type;
        public DateTime LastModified;
    }

    [SerializeField] private List<Entry> entries = new();

    public IReadOnlyList<Entry> Entries => entries;

    public void UpdateEntry(string key, object value)
    {
        var type = value.GetType().Name;
        var valStr = value.ToString();

        int index = entries.FindIndex(e => e.Key == key);
        if (index >= 0)
        {
            entries[index] = new Entry
            {
                Key = key,
                Value = valStr,
                Type = type,
                LastModified = DateTime.Now
            };
        }
        else
        {
            entries.Add(new Entry
            {
                Key = key,
                Value = valStr,
                Type = type,
                LastModified = DateTime.Now
            });
        }
    }

    public void RemoveEntry(string key) => entries.RemoveAll(e => e.Key == key);
    public void ClearAll() => entries.Clear();
}