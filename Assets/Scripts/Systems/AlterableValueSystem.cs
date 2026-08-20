using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HypnicEmpire
{
    public static class AlterableValueSystem
    {
        public static Dictionary<string, AlterableValue> ValueMap = new();

        public static AlterableValue GetAlterableValue(string name)
        {
            return ValueMap.ContainsKey(name) ? ValueMap[name] : new AlterableValue() { Name = "UNKNOWN" };
        }

        //  For callers that write to a value rather than read one: the placeholder GetAlterableValue hands back
        //  for an unknown name swallows the write silently, which hides a name that no longer matches anything
        //  authored. Answering whether the value exists lets the caller pass over the ones that do not.
        public static bool TryGetAlterableValue(string name, out AlterableValue alterableValue)
        {
            return ValueMap.TryGetValue(name ?? "", out alterableValue);
        }

        public static int GetAlterableValueCurrentVal(string name)
        {
            return ValueMap.ContainsKey(name) ? ValueMap[name].CurrentValue : 0;
        }

        private static bool IsAlterableValueValid(AlterableValue aVal)
        {
            if (aVal.MinimumValue > aVal.CurrentValue) return false;
            if (aVal.MaximumValue < aVal.CurrentValue) return false;
            return true;
        }

        public static void LoadAllAlterableValues(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var alterableValueList = JsonSerialization.Deserialize<List<AlterableValue>>(jsonContent);

                    ValueMap.Clear();
                    foreach (var alterableValue in alterableValueList)
                        if (IsAlterableValueValid(alterableValue))
                            ValueMap[alterableValue.Name] = alterableValue;
                        else
                            Debug.LogWarning($"Attempting to add invalid AlterableValue entry {alterableValue.Name}");

                    Debug.Log($"Successfully loaded {ValueMap.Count} AlterableValue entries from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading AlterableValue from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"AlterableValue.json not found at {jsonFilePath}");
            }
        }
    }
}