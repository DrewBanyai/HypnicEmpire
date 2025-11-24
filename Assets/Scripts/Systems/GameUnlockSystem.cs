using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HypnicEmpire
{
    public static class GameUnlockSystem
    {
        private static List<string> UnlockIDs = new();
        private static Dictionary<GameUnlock, List<Action<bool>>> UnlockActionMap = new();

        public static void AddGameUnlockAction(GameUnlock unlock, Action<bool> action)
        {
            if (!UnlockActionMap.ContainsKey(unlock))
                UnlockActionMap[unlock] = new();
            UnlockActionMap[unlock].Add(action);
        }

        public static void SetUnlockValue(GameUnlock unlock, bool unlocked)
        {
            if (UnlockActionMap.ContainsKey(unlock))
                foreach (var action in UnlockActionMap[unlock])
                    action?.Invoke(unlocked);
        }

        public static bool IsUnlockIDValid(string unlockID) { return UnlockIDs.Contains(unlockID); }

        public static void LoadAllUnlockIDs(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var unlockIDs = JsonSerialization.Deserialize<List<string>>(jsonContent);

                    UnlockIDs.Clear();
                    foreach (string unlockID in unlockIDs)
                        if (!IsUnlockIDValid(unlockID))
                            UnlockIDs.Add(unlockID);
                        else
                            Debug.LogWarning($"Attempting to add already existing UnlockID value {unlockID}");

                    Debug.Log($"Successfully loaded {unlockIDs.Count} UnlockIDs from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading UnlockIDs from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"UnlockIDs.json not found at {jsonFilePath}");
            }
        }
    }
}