using System.Collections.Generic;

namespace HypnicEmpire
{
    public class BuildingData
    {
        public string Name;
        public string Text;
        public string BuildingIcon;
        public BuildingStartingCount StartingCount;
        public List<BuildingCostTier> Costs;
        public List<BuildingAlteredValue> AlteredValues;
        public List<BuildingUpgrade> Upgrades;
    }

    public class BuildingStartingCount
    {
        public int Amount;
        public string Note;
    }

    public class BuildingCostTier
    {
        public int Count;
        public List<BuildingResourceCost> Cost;
    }

    public class BuildingResourceCost
    {
        public string ResourceType;
        public ResourceValue ResourceValue;
        public int Amount; // To handle both field names found in JSON
    }

    public class BuildingAlteredValue
    {
        public string ValueName;
        public int Amount;
        public string Trigger;
    }

    public class BuildingUpgrade
    {
        public string Trigger;
        public string Effect;
    }

    public class BuildingGlobalEffect
    {
        public string Trigger;
        public string Effect;
    }

    public class BuildingsDataContainer
    {
        public List<BuildingGlobalEffect> GlobalEffects;
        public List<BuildingResourceCost> LandCost;
        public List<BuildingData> BuildingTypes;
    }
}
