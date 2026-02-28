using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

namespace HypnicEmpire
{
    public static class JournalEntrySystem
    {
        public static Dictionary<string, JournalEntryData> JournalEntryDataMap = new();
        public static List<string> ShownJournalEntries = new();
        public static event Action<string> OnJournalEntryAdded;

        public static void AddJournalEntry(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ShownJournalEntries.Add(text);
            OnJournalEntryAdded?.Invoke(text);
        }

        public static void LoadAllJournalEntries(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var journalEntryDataList = JsonSerialization.Deserialize<List<JournalEntryData>>(jsonContent);

                    JournalEntryDataMap.Clear();
                    foreach (JournalEntryData entryData in journalEntryDataList)
                        if (GameUnlockSystem.IsUnlockIDValid(entryData.Trigger))
                            JournalEntryDataMap[entryData.Trigger] = entryData;
                        else
                            Debug.LogWarning($"Attempting to add a JournalEntryData with a trigger of {entryData.Trigger} but that is not a valid UnlockID");

                    Debug.Log($"Successfully loaded {JournalEntryDataMap.Count} JournalEntryDatas from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading JournalEntryDatas from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"JournalEntries.json not found at {jsonFilePath}");
            }
        }
    }
}