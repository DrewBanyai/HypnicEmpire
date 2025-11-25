using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HypnicEmpire
{
    public static class DevelopmentSystem
    {
        public static List<DevelopmentUnlockMultiplier> CostMultiplierUnlocks = new();
        public static List<DevelopmentDataEntry> DevelopmentEntries = new();

        private static bool IsCostMultiplierUnlockValid(DevelopmentUnlockMultiplier mult)
        {
            return true;
        }

        private static bool IsDevelopmentEntryValid(DevelopmentDataEntry entry)
        {
            return true;
        }

        public static void LoadAllDevelopments(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var developmentData = JsonSerialization.Deserialize<DevelopmentData>(jsonContent);

                    CostMultiplierUnlocks.Clear();
                    foreach (var cmUnlock in developmentData.CostMultiplierUnlocks)
                        if (IsCostMultiplierUnlockValid(cmUnlock))
                            CostMultiplierUnlocks.Add(cmUnlock);
                        else
                            Debug.LogWarning($"Attempting to add invalid DevelopmentUnlockMultiplier value {cmUnlock.Unlock}");

                    Debug.Log($"Successfully loaded {CostMultiplierUnlocks.Count} DevelopmentUnlockMultipliers from {jsonFilePath}");

                    DevelopmentEntries.Clear();
                    foreach (var devEntry in developmentData.DevelopmentEntries)
                        if (IsDevelopmentEntryValid(devEntry))
                            DevelopmentEntries.Add(devEntry);
                        else
                            Debug.LogWarning($"Attempting to add invalid DevelopmentDataEntry value {devEntry.Title}");

                    Debug.Log($"Successfully loaded {DevelopmentEntries.Count} DevelopmentDataEntries from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading DevelopmentDataEntries from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Developments.json not found at {jsonFilePath}");
            }
        }
    }
}