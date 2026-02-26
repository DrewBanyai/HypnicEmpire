using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class ResourceUnlockTrigger
    {
        public int Value;
        public string Operator;
        public string Unlock;
    }

    public class UnlockToResourceTypeData
    {
        public string Unlock;
        public string ResourceType;
    }

    public class ResourceAlteration
    {
        public double MaxAdditive;
        public double MaxMultiplier = 1.0;
    }

    public class ResourceTypeData
    {
        public string Name;
        public int InitialValue;
        public int InitialMaximum;
        public string ResourceGroup;
        public List<string> Upgrades; // Note: An upgrade will eventually be a class?
        public List<ResourceUnlockTrigger> Unlocks;
        public SerializableDictionary<string, ResourceAlteration> UnlockAlterations;

        public int GetMaximum()
        {
            double max = InitialMaximum;
            if (UnlockAlterations != null)
            {
                var unlockedAlterations = UnlockAlterations.Where(ua => GameUnlockSystem.IsUnlocked(ua.Key));
                foreach (var entry in unlockedAlterations)
                {
                    max += entry.Value.MaxAdditive;
                    max *= entry.Value.MaxMultiplier;
                }
            }
            return (int)max;
        }
    }

    public class ResourceData
    {
        public List<UnlockToResourceTypeData> UnlockToResourceTypes;
        public List<string> ResourceGroups;
        public List<ResourceTypeData> ResourceTypes;
    }
}