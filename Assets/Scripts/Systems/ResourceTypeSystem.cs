using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HypnicEmpire
{
    public static class ResourceTypeSystem
    {
        public static List<string> ResourceTypes = new();
        public static ResourceData ResourceData = new();

        public static string GetResourceTypeFromUnlock(string unlock)
        {
            var foundEntry = ResourceData.UnlockToResourceTypes.Find(utrt => utrt.Unlock == unlock);
            return foundEntry?.ResourceType;
        }

        public static string GetUnlockFromResourceType(string resourceType)
        {
            var foundEntry = ResourceData.UnlockToResourceTypes.Find(utrt => utrt.ResourceType == resourceType);
            return foundEntry?.Unlock;
        }

        public static void LoadAllResourceTypes(string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    ResourceData = JsonSerialization.Deserialize<ResourceData>(jsonContent);

                    ResourceTypes.Clear();
                    foreach (var rt in ResourceData.ResourceTypes)
                    {
                        if (!ResourceData.ResourceGroups.Contains(rt.ResourceGroup))
                            Debug.Log($"Resource {rt.Name} loaded with resource group listed as '{rt.ResourceGroup} which does not exist in ResourceGroups list");
                        ResourceTypes.Add(rt.Name);
                    }

                    Debug.Log($"Successfully loaded {ResourceData.ResourceGroups.Count} Resource Groups and {ResourceData.ResourceTypes.Count} Resource Types from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading Resource Data from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Resources.json not found at {jsonFilePath}");
            }
        }
    }
}