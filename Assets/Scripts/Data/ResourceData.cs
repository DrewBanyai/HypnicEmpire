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
            // Base storage + additive "AddMax" storage modifiers (building/project driven),
            // then the resource's own unlock alterations. Modifier storage is folded in before
            // the unlock multipliers so a storage-doubling unlock also scales building storage.
            double max = InitialMaximum + ModifierValueSystem.GetResourceMaxAdditive(Name, ResourceGroup);
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