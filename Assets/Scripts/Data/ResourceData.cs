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

    public class ResourceTypeData
    {
        public string Name;
        public int InitialValue;
        public int InitialMaximum;
        public string ResourceGroup;
        public List<string> Upgrades; // Note: An upgrade will eventually be a class?
        public List<ResourceUnlockTrigger> Unlocks;

        public int GetMaximum() { return InitialMaximum; } //  TODO: Eventually use an override function to grab the CURRENT maximum based on upgrades
    }

    public class ResourceData
    {
        public List<UnlockToResourceTypeData> UnlockToResourceTypes;
        public List<string> ResourceGroups;
        public List<ResourceTypeData> ResourceTypes;
    }
}