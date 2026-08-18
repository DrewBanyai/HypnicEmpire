using System.Collections.Generic;

namespace HypnicEmpire
{
    public class BuildingData
    {
        public string Name;
        public string Text;
        public string BuildingIcon;
        //  Unlock that reveals this building. Empty means always revealed.
        public string RevealUnlock;
        public BuildingStartingCount StartingCount;
        public List<BuildingCostTier> Costs;
        public List<AlteredValueData> AlteredValues;
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

        //  Whichever of the two field names the entry was authored with.
        public ResourceValue GetValue() { return ResourceValue ?? new ResourceValue(Amount); }
    }

    //  A flat contribution to a modifier AlterableValue, optionally gated behind an unlock. Shared by
    //  everything that can raise a value: buildings pay it per building built, developments once each.
    public class AlteredValueData
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

    //  A rule that applies once its Trigger is unlocked, rather than per building. Effect describes it
    //  for readers; only the fields below it are acted on.
    public class BuildingGlobalEffect
    {
        public string Trigger;
        public string Effect;

        //  Land handed over outright by the unlock, as opposed to land the player buys.
        public int LandGranted;
    }

    public class BuildingsDataContainer
    {
        public List<BuildingGlobalEffect> GlobalEffects;
        public List<BuildingResourceCost> LandCost;
        public List<BuildingData> BuildingTypes;
    }
}
