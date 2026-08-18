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

        //  The unlock listener registered per trigger, kept so it can be withdrawn again on re-initialization.
        private static readonly Dictionary<string, Action<bool>> UnlockListeners = new();

        public static void AddJournalEntry(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ShownJournalEntries.Add(text);
            GameController.CurrentGameState.JournalEntries.Add(text);
            OnJournalEntryAdded?.Invoke(text);
        }

        //  Idempotent: the previously registered listeners are withdrawn first, so calling this again (after a
        //  load or reset, or after journal data is reloaded) leaves exactly one listener per trigger instead of
        //  stacking a second set that would write every entry twice.
        public static void InitializeListeners()
        {
            ClearListeners();

            foreach (var journalEntry in JournalEntryDataMap)
            {
                string trigger = journalEntry.Key;
                JournalEntryData entryData = journalEntry.Value;

                //  Registered as a "before" action so the entry is written only on the transition into
                //  unlocked: by the time the "after" actions run, the unlock already reads as true.
                Action<bool> listener = (bool unlocked) =>
                {
                    if (!unlocked) return;
                    if (GameUnlockSystem.IsUnlocked(trigger)) return;
                    if (entryData.Text == null || entryData.Text.Count == 0) return;

                    AddJournalEntry(entryData.Text[UnityEngine.Random.Range(0, entryData.Text.Count)]);
                };

                UnlockListeners[trigger] = listener;
                GameUnlockSystem.AddGameUnlockAction(trigger, true, listener);
            }
        }

        public static void ClearListeners()
        {
            foreach (var listener in UnlockListeners)
                GameUnlockSystem.RemoveGameUnlockAction(listener.Key, true, listener.Value);

            UnlockListeners.Clear();
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