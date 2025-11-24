using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HypnicEmpire
{
    public class LevelDataSystem
    {
        private static List<LevelGrouping> LoadedLevelGroupings = new();
        private static Dictionary<int, LevelDataEntry> LevelDataMap = new();

        private static bool IsLeveLGroupingValidToAdd(LevelGrouping levelGrouping)
        {
            if (levelGrouping.Min < 0) return false;
            if (levelGrouping.Max > 10000) return false;
            if (levelGrouping.Min > levelGrouping.Max) return false;

            foreach (var loadedLG in LoadedLevelGroupings)
            {
                if (levelGrouping.Min >= loadedLG.Min && levelGrouping.Min <= loadedLG.Max) return false;
                if (levelGrouping.Max <= loadedLG.Max && levelGrouping.Max >= loadedLG.Min) return false;
            }
            
            return true;
        }

        private static bool IsLevelWithinGroupingData(int level)
        {
            foreach (var levelGrouping in LoadedLevelGroupings)
                if (level >= levelGrouping.Min && level <= levelGrouping.Max)
                    return true;

            return false;
        }
        
        private static bool IsLevelDataEntryValidToAdd(LevelDataEntry levelDataEntry)
        {
            if (!IsLevelWithinGroupingData(levelDataEntry.Level)) return false;
            if (levelDataEntry.DelveCount <= 0) return false;

            if (LevelDataMap.ContainsKey(levelDataEntry.Level)) return false;

            return true;
        }

        public static void LoadAllLevelData(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var levelDataStruct = JsonSerialization.Deserialize<LevelData>(jsonContent);


                    LoadedLevelGroupings.Clear();
                    foreach (var levelGrouping in levelDataStruct.LevelGroupings)
                    {
                        string spritePath = $"GameLevelImages/{Path.GetFileNameWithoutExtension(levelGrouping.Image)}";
                        levelGrouping.ImageSprite = Resources.Load<Sprite>(spritePath);

                        if (IsLeveLGroupingValidToAdd(levelGrouping))
                            LoadedLevelGroupings.Add(levelGrouping);
                        else
                            Debug.LogWarning($"Attempting to add invalid LevelGrouping data: {levelGrouping.Name}");
                    }
                            
                    Debug.Log($"Successfully loaded {LoadedLevelGroupings.Count} LevelGrouping data entries from {jsonFilePath}");

                    LevelDataMap.Clear();
                    foreach (var levelDataEntry in levelDataStruct.LevelDataEntries)
                        if (IsLevelDataEntryValidToAdd(levelDataEntry))
                            LevelDataMap[levelDataEntry.Level] = levelDataEntry;
                        else
                            Debug.LogWarning($"Attempting to add invalid LevelDataEntry data: {levelDataEntry.Level}");

                    Debug.Log($"Successfully loaded {LevelDataMap.Count} LevelDataEntry objects from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading LevelGrouping data entries from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"LevelData.json not found at {jsonFilePath}");
            }
        }
    }
}