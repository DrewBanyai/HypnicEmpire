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

        public static void InitializeListeners()
        {
            foreach (var achievement in AchievementDataMap.Values)
            {
                string trigger = achievement.Trigger;
                GameUnlockSystem.AddGameUnlockAction(trigger, true, (bool unlocked) =>
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
                });
            }
        }

        public static AchievementData GetAchievementByName(string name)
        {
            foreach (var achievement in AchievementDataMap.Values)
                if (achievement.Name == name) return achievement;
            return null;
        }
    }
}