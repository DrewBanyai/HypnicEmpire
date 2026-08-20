using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HypnicEmpire
{
    public static class ActionTimingSystem
    {
        public static ActionTimingConfiguration Configuration { get; private set; }

        private static readonly Dictionary<string, ActionTimingData> ActionMap = new();

        public static bool TryGetAction(string actionName, out ActionTimingData timing)
        {
            return ActionMap.TryGetValue(actionName, out timing);
        }

        public static bool LoadConfiguration(string jsonFilePath)
        {
            Configuration = null;
            ActionMap.Clear();

            if (!File.Exists(jsonFilePath))
            {
                Debug.LogError($"Action timing configuration not found at {jsonFilePath}");
                return false;
            }

            try
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                var configuration = JsonSerialization.Deserialize<ActionTimingConfiguration>(jsonContent);
                if (!TryValidate(configuration, out string error))
                {
                    Debug.LogError($"Invalid action timing configuration at {jsonFilePath}: {error}");
                    return false;
                }

                foreach (var action in configuration.Actions)
                    ActionMap.Add(action.Name, action);

                Configuration = configuration;
                Debug.Log($"Successfully loaded timing for {ActionMap.Count} actions from {jsonFilePath}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Error loading action timing configuration from {jsonFilePath}: {exception.Message}");
                return false;
            }
        }

        private static bool TryValidate(ActionTimingConfiguration configuration, out string error)
        {
            if (configuration == null)
            {
                error = "deserialized configuration is null";
                return false;
            }

            if (configuration.TimeScale <= 0f)
            {
                error = "TimeScale must be greater than zero";
                return false;
            }

            if (configuration.PathVisualizationWorkers < 0)
            {
                error = "PathVisualizationWorkers cannot be negative";
                return false;
            }

            if (configuration.Actions == null || configuration.Actions.Count == 0)
            {
                error = "Actions must contain at least one entry";
                return false;
            }

            var names = new HashSet<string>();
            foreach (var action in configuration.Actions)
            {
                if (action == null || string.IsNullOrEmpty(action.Name))
                {
                    error = "every action timing entry must have a Name";
                    return false;
                }

                if (!names.Add(action.Name))
                {
                    error = $"duplicate action timing entry '{action.Name}'";
                    return false;
                }

                if (action.ProgressMaximum <= 0.0)
                {
                    error = $"{action.Name}.ProgressMaximum must be greater than zero";
                    return false;
                }

                if (action.BaseProgressPerSecond <= 0.0)
                {
                    error = $"{action.Name}.BaseProgressPerSecond must be greater than zero";
                    return false;
                }

                if (action.ProgressPerWorkerPerSecond < 0.0)
                {
                    error = $"{action.Name}.ProgressPerWorkerPerSecond cannot be negative";
                    return false;
                }

                if (action.UnlockSpeedMultipliers == null) continue;
                foreach (var multiplier in action.UnlockSpeedMultipliers)
                {
                    if (string.IsNullOrEmpty(multiplier.Key) || multiplier.Value <= 0.0)
                    {
                        error = $"{action.Name} has an invalid unlock speed multiplier";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
