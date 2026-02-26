using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HypnicEmpire
{
    public static class GameUnlockSystem
    {
        public static List<string> UnlockIDs = new();
        private static Dictionary<string, List<Action<bool>>> UnlockActionMapBefore = new();
        private static Dictionary<string, List<Action<bool>>> UnlockActionMapAfter = new();
        public static SerializableDictionary<string, bool> GameUnlockList = new();

        public static void AddGameUnlockAction(string unlock, bool before, Action<bool> action)
        {
            var actionMap = before ? UnlockActionMapBefore : UnlockActionMapAfter;
            if (!actionMap.ContainsKey(unlock))
                actionMap[unlock] = new();
            actionMap[unlock].Add(action);
        }

        public static void SetUnlockValue(string unlock, bool unlocked)
        {
            if (UnlockActionMapBefore.ContainsKey(unlock))
                foreach (var action in UnlockActionMapBefore[unlock])
                    action?.Invoke(unlocked);
            GameUnlockList[unlock] = unlocked;
            if (UnlockActionMapAfter.ContainsKey(unlock))
                foreach (var action in UnlockActionMapAfter[unlock])
                    action?.Invoke(unlocked);
        }

        public static bool IsUnlocked(string unlockID)
        {
            return GameUnlockList.ContainsKey(unlockID) ? GameUnlockList[unlockID] : false;
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