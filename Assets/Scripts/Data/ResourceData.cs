using System.Collections.Generic;

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

        //  Whether this alteration counts once, against the storage the resource was authored with, rather
        //  than continuously against whatever it currently holds. A continuous multiplier is re-applied every
        //  time the maximum is worked out, so it goes on scaling every storage increase earned after it too;
        //  a one-off one lifts the authored base and leaves everything won later untouched.
        public bool AppliesOnce;
    }

    //  A value the player never holds or spends but which the resource list shows anyway ("People" above
    //  all): it is accumulated elsewhere, as an AlterableValue, and is named here only so the list knows
    //  which section it belongs to and where it sits in that section.
    public class DerivedValueTypeData
    {
        public string Name;
        public string ResourceGroup;
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

        // The order storage is built up in is what decides which increases each alteration catches.
        // One-off alterations fold onto the authored base alone; the additive "AddMax" storage that
        // buildings and projects grant is laid on after them, out of their reach; continuous
        // alterations fold over that whole total, so they keep scaling with it.
        public int GetMaximum()
        {
            double max = ApplyAlterations(InitialMaximum, appliesOnce: true)
                       + ModifierValueSystem.GetResourceMaxAdditive(Name, ResourceGroup);
            return (int)ApplyAlterations(max, appliesOnce: false);
        }

        private double ApplyAlterations(double max, bool appliesOnce)
        {
            if (UnlockAlterations == null) return max;

            foreach (var entry in UnlockAlterations)
            {
                if (entry.Value == null || entry.Value.AppliesOnce != appliesOnce) continue;
                if (!GameUnlockSystem.IsUnlocked(entry.Key)) continue;

                max += entry.Value.MaxAdditive;
                max *= entry.Value.MaxMultiplier;
            }
            return max;
        }
    }

    public class ResourceData
    {
        public List<UnlockToResourceTypeData> UnlockToResourceTypes;

        //  Doubles as the order the resource list shows its sections in.
        public List<string> ResourceGroups;

        public List<DerivedValueTypeData> DerivedValueTypes;
        public List<ResourceTypeData> ResourceTypes;
    }
}