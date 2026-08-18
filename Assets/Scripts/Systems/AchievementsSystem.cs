using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEngine;

namespace HypnicEmpire
{
    public static class AchievementsSystem
    {
        public static Dictionary<string, AchievementData> AchievementDataMap = new();
        public static List<string> UnlockedAchievements = new();
        public static int ProgressBoostPercent = 100;

        //  The unlock listener registered per trigger, kept so it can be withdrawn again on re-initialization.
        private static readonly Dictionary<string, Action<bool>> UnlockListeners = new();

        public static event Action<string> OnAchievementUnlocked;

        public static double GetProgressBoostMultiplier() { return ProgressBoostPercent / 100.0; }

        public static void LoadAllAchievementsData(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var achievementDataList = JsonSerialization.Deserialize<List<AchievementData>>(jsonContent);

                    AchievementDataMap.Clear();
                    foreach (AchievementData entryData in achievementDataList)
                    {
                        if (GameUnlockSystem.IsUnlockIDValid(entryData.Trigger))
                        {
                            //  Load the sprite from Resources
                            string spritePath = $"AchievementIcons/{Path.GetFileNameWithoutExtension(entryData.Image)}";
                            entryData.ImageSprite = Resources.Load<Sprite>(spritePath);
                            
                            AchievementDataMap[entryData.Trigger] = entryData;
                        }
                        else
                        {
                            Debug.LogWarning($"Attempting to add a AchievementData with a trigger of {entryData.Trigger} but that is not a valid UnlockID");
                        }
                    }

                    Debug.Log($"Successfully loaded {AchievementDataMap.Count} AchievementData (with {AchievementDataMap.Values.Count(a => a.ImageSprite != null)} sprites) from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading AchievementDatas from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Achievements.json not found at {jsonFilePath}");
            }
        }

        //  Idempotent: the previously registered listeners are withdrawn first, so calling this again (after a
        //  load or reset, or after achievement data is reloaded) leaves exactly one listener per trigger
        //  instead of stacking a second set that would announce every unlock twice.
        public static void InitializeListeners()
        {
            ClearListeners();

            foreach (var achievement in AchievementDataMap.Values)
            {
                string trigger = achievement.Trigger;
                Action<bool> listener = (bool unlocked) =>
                {
                    if (unlocked)
                    {
                        if (!UnlockedAchievements.Contains(trigger))
                        {
                            UnlockedAchievements.Add(trigger);
                            JournalEntrySystem.AddJournalEntry("ACHIEVEMENT UNLOCKED - " + achievement.Name);
                            OnAchievementUnlocked?.Invoke(trigger);
                        }
                    }
                };

                UnlockListeners[trigger] = listener;
                GameUnlockSystem.AddGameUnlockAction(trigger, true, listener);
            }
        }

        //  Earned achievements are a projection of the unlock values, so they are dropped whenever those are:
        //  left behind, they would outlive the unlocks that awarded them and show as earned in a fresh game.
        public static void ClearUnlockedAchievements()
        {
            UnlockedAchievements.Clear();
        }

        public static void ClearListeners()
        {
            foreach (var listener in UnlockListeners)
                GameUnlockSystem.RemoveGameUnlockAction(listener.Key, true, listener.Value);

            UnlockListeners.Clear();
        }

        public static AchievementData GetAchievementByName(string name)
        {
            foreach (var achievement in AchievementDataMap.Values)
                if (achievement.Name == name) return achievement;
            return null;
        }
    }
}