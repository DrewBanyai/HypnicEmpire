using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
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
        public List<string> Unlocks; // Note: An unlock will eventually be a class?
    }

    public class ResourceData
    {
        public List<UnlockToResourceTypeData> UnlockToResourceTypes;
        public List<string> ResourceGroups;
        public List<ResourceTypeData> ResourceTypes;
    }
}