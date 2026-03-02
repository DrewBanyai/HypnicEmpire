using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HypnicEmpire
{
    public static class BuildingDataSystem
    {
        public static BuildingsDataContainer Data = new();

        public static void LoadAllBuildingsData(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    Data = JsonSerialization.Deserialize<BuildingsDataContainer>(jsonContent);
                    Debug.Log($"Successfully loaded {Data.BuildingTypes.Count} Building Types from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading Buildings Data from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Buildings.json not found at {jsonFilePath}");
            }
        }

        public static BuildingData GetBuildingData(string name)
        {
            return Data.BuildingTypes?.Find(b => b.Name == name);
        }
    }
}
