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
            if (action == null) return;

            var actionMap = before ? UnlockActionMapBefore : UnlockActionMapAfter;
            if (!actionMap.ContainsKey(unlock))
                actionMap[unlock] = new();
            actionMap[unlock].Add(action);
        }

        //  A subscriber whose listeners are rebuilt (a system re-initialized after a load or reset) must be
        //  able to withdraw the old ones, otherwise its registrations stack and a single unlock invokes them
        //  once per registration. Registration is not deduplicated, so a subscriber removes exactly the
        //  delegate instances it added.
        public static void RemoveGameUnlockAction(string unlock, bool before, Action<bool> action)
        {
            if (action == null) return;

            var actionMap = before ? UnlockActionMapBefore : UnlockActionMapAfter;
            if (actionMap.TryGetValue(unlock, out var actions))
                actions.Remove(action);
        }

        public static void SetUnlockValue(string unlock, bool unlocked)
        {
            InvokeUnlockActions(UnlockActionMapBefore, unlock, unlocked);
            GameUnlockList[unlock] = unlocked;
            InvokeUnlockActions(UnlockActionMapAfter, unlock, unlocked);
        }

        //  Dispatched over a copy so that an action is free to add or remove unlock actions while it runs
        //  (buying a development registers the actions of what it opens up) without invalidating iteration.
        private static void InvokeUnlockActions(Dictionary<string, List<Action<bool>>> actionMap, string unlock, bool unlocked)
        {
            if (!actionMap.TryGetValue(unlock, out var actions) || actions.Count == 0) return;

            foreach (var action in actions.ToArray())
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