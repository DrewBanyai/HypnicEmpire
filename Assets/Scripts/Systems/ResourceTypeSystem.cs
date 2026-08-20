using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HypnicEmpire
{
    //  Where a name shown in the resource list sits: which section it belongs to, by the authored order of
    //  the resource groups, and its place within that section.
    public readonly struct ResourceDisplayPosition
    {
        public readonly int SectionIndex;
        public readonly int MemberIndex;

        public ResourceDisplayPosition(int sectionIndex, int memberIndex)
        {
            SectionIndex = sectionIndex;
            MemberIndex = memberIndex;
        }
    }

    public static class ResourceTypeSystem
    {
        public static List<string> ResourceTypes = new();
        public static ResourceData ResourceData = new();

        private static readonly List<UnlockToResourceTypeData> NoUnlockToResourceTypes = new();

        //  Worked out once as the data loads, so ordering the list costs nothing but a lookup per row: a
        //  name's section is where its resource group appears in ResourceGroups, and its place within that
        //  section is the order the name itself is authored in.
        private static readonly Dictionary<string, ResourceDisplayPosition> DisplayPositions = new();

        //  The whole mapping set, for callers that have to replay it (rebuilding the resource list from a
        //  loaded unlock state) rather than resolve a single unlock. Empty until Resources.json loads.
        public static IReadOnlyList<UnlockToResourceTypeData> UnlockToResourceTypes =>
            ResourceData?.UnlockToResourceTypes ?? NoUnlockToResourceTypes;

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

        public static bool TryGetDisplayPosition(string name, out ResourceDisplayPosition position)
        {
            return DisplayPositions.TryGetValue(name ?? "", out position);
        }

        //  How many sections the resource list has to hold, whether or not anything is unlocked to show in
        //  them: the list keeps a place for every group so a row never moves once it has appeared.
        public static int ResourceGroupCount => ResourceData?.ResourceGroups?.Count ?? 0;

        public static string GetResourceGroupName(int sectionIndex)
        {
            var groups = ResourceData?.ResourceGroups;
            return groups != null && sectionIndex >= 0 && sectionIndex < groups.Count ? groups[sectionIndex] : "";
        }

        //  Resources first and then the derived values, each in the order it is authored, so a section that
        //  holds both reads down the file. A name whose group is not declared is left unplaced rather than
        //  guessed at: the list shows it after the authored sections instead of dropping it.
        private static void BuildDisplayPositions()
        {
            DisplayPositions.Clear();

            var groups = ResourceData?.ResourceGroups;
            if (groups == null) return;

            var nextMemberIndex = new int[groups.Count];

            void Place(string name, string resourceGroup)
            {
                int sectionIndex = groups.IndexOf(resourceGroup);
                if (string.IsNullOrEmpty(name) || sectionIndex < 0) return;

                DisplayPositions[name] = new ResourceDisplayPosition(sectionIndex, nextMemberIndex[sectionIndex]);
                nextMemberIndex[sectionIndex]++;
            }

            if (ResourceData.ResourceTypes != null)
                foreach (var rt in ResourceData.ResourceTypes)
                    Place(rt.Name, rt.ResourceGroup);

            if (ResourceData.DerivedValueTypes != null)
                foreach (var dv in ResourceData.DerivedValueTypes)
                    Place(dv.Name, dv.ResourceGroup);
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

                    if (ResourceData.DerivedValueTypes != null)
                        foreach (var dv in ResourceData.DerivedValueTypes)
                            if (!ResourceData.ResourceGroups.Contains(dv.ResourceGroup))
                                Debug.Log($"Derived value {dv.Name} loaded with resource group listed as '{dv.ResourceGroup}' which does not exist in ResourceGroups list");

                    BuildDisplayPositions();

                    GameSubscriptionSystem.CreateResourceTypeSubscriptionMaps();
                    SubscribeToResourceUnlocks();
                    SubscribeToAlterationUnlocks();

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

        private static void SubscribeToAlterationUnlocks()
        {
            foreach (var rt in ResourceData.ResourceTypes)
            {
                if (rt.UnlockAlterations == null) continue;

                foreach (var unlockKey in rt.UnlockAlterations.Keys)
                {
                    string resourceName = rt.Name;
                    GameUnlockSystem.AddGameUnlockAction(unlockKey, false, (bool unlocked) => {
                        UpdateResourceMaximum(resourceName);
                    });
                }
            }
        }

        private static void UpdateResourceMaximum(string resourceType)
        {
            var resourceTypeData = ResourceData.ResourceTypes.Find(rt => rt.Name == resourceType);
            if (resourceTypeData == null) return;

            int newMax = resourceTypeData.GetMaximum();
            GameController.CurrentGameState.SetResourceMaximum(resourceType, newMax);
        }

        // Recompute every resource's maximum from GetMaximum() and push into the game state.
        // Called by ModifierValueSystem after storage modifiers change (building/project effects).
        public static void RefreshAllResourceMaxima()
        {
            if (GameController.CurrentGameState == null || ResourceData?.ResourceTypes == null) return;
            foreach (var rt in ResourceData.ResourceTypes)
                GameController.CurrentGameState.SetResourceMaximum(rt.Name, rt.GetMaximum());
        }

        private static void SubscribeToResourceUnlocks()
        {
            foreach (string resourceType in ResourceTypes)
            {
                GameSubscriptionSystem.SubscribeToResourceAmount(resourceType, (ResourceValue addAmount, ResourceValue currentAmount) => {
                    var resourceTypeData = ResourceData.ResourceTypes.Find(rt => rt.Name == resourceType);
                    if (resourceTypeData == null) return;

                    foreach (var ul in resourceTypeData.Unlocks)
                    {
                        switch (ul.Operator)
                        {
                            case "==":
                                if (currentAmount == ul.Value)
                                    GameUnlockSystem.SetUnlockValue(ul.Unlock, true);
                                break;
                            case "<=":
                                if (currentAmount <= ul.Value)
                                    GameUnlockSystem.SetUnlockValue(ul.Unlock, true);
                                break;
                            case ">=":
                                if (currentAmount >= ul.Value)
                                    GameUnlockSystem.SetUnlockValue(ul.Unlock, true);
                                break;
                            case "MAX":
                                if (currentAmount >= resourceTypeData.GetMaximum())
                                    GameUnlockSystem.SetUnlockValue(ul.Unlock, true);
                                break;
                        }
                    }
                });
            }
        }
    }
}