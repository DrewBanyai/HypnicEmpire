using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class ResourceChangeAlteration
    {
        public string ResourceType;
        public double Multiplier;
        public double Additive;
    }

    public class TaskActionAlteration
    {
        public List<ResourceChangeAlteration> CostChanges;
        public List<ResourceChangeAlteration> RewardChanges;
    }

    public class TaskActionValueDeterminant
    {
        public SerializableDictionary<string, TaskActionAlteration> UnlockAlterations;

        public List<ResourceAmountData> GetResourceChange(List<ResourceAmountData> originalChange)
        {
            List<ResourceAmountData> resourceChange = CopyResourceChange(originalChange);
            if (UnlockAlterations == null) return resourceChange;

            var unlockedAlterations = UnlockAlterations.Where(ua => ua.Value != null && GameUnlockSystem.IsUnlocked(ua.Key)).Select(ua => ua.Value).ToList();
            var unlockedCostAlterations = unlockedAlterations.Where(ua => ua.CostChanges != null && ua.CostChanges.Count != 0).ToList();
            var unlockedRewardAlterations = unlockedAlterations.Where(ua => ua.RewardChanges != null && ua.RewardChanges.Count != 0).ToList();
            
            foreach (var rcAmount in resourceChange)
            {
                if (rcAmount.ResourceValue < 0.0)
                {
                    foreach (var ura in unlockedCostAlterations)
                        foreach (var rChange in ura.CostChanges)
                            if (rChange != null && rChange.ResourceType == rcAmount.ResourceType)
                            {
                                rcAmount.ResourceValue += rChange.Additive;
                                rcAmount.ResourceValue *= rChange.Multiplier;
                            }
                }
                else if (rcAmount.ResourceValue > 0.0)
                {
                    foreach (var ura in unlockedRewardAlterations)
                        foreach (var rChange in ura.RewardChanges)
                            if (rChange != null && rChange.ResourceType == rcAmount.ResourceType)
                            {
                                rcAmount.ResourceValue += rChange.Additive;
                                rcAmount.ResourceValue *= rChange.Multiplier;
                            }
                }
            }

            return resourceChange;
        }

        // Alterations are applied to a copy so the loaded ResourceChange data is never mutated.
        public static List<ResourceAmountData> CopyResourceChange(List<ResourceAmountData> originalChange)
        {
            List<ResourceAmountData> resourceChange = new();
            if (originalChange == null) return resourceChange;
            foreach (var c in originalChange)
                if (c != null)
                    resourceChange.Add(new ResourceAmountData(c.ResourceType, c.ResourceValue));
            return resourceChange;
        }
    }

    public class TaskActionData
    {
        public string Name;
        public string DisplayName;
        public string ActionSection;
        public TaskActionValueDeterminant ValueDeterminant;
        public List<ResourceAmountData> ResourceChange;
    }

    public class TaskUnlockAndActionData
    {
        public SerializableDictionary<string, string> UnlockToActionMap;
        public List<TaskActionData> ActionData;
    }
}