using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    //  Buying buildings. What a building costs rises with how many of it already stand, so the price of
    //  the next one is read from the cost tier the current count falls in.
    //
    //  Land is authored alongside the resource costs but is not one of them: it is never held in the
    //  game state, and LandSystem derives the land in use from the building counts. A purchase
    //  therefore only has to find enough free land, and the count it goes on to change accounts for it.
    public static class BuildingPurchaseSystem
    {
        //  Raised after a building has been built and named it. Counts are owned by
        //  ModifierValueSystem, whose recompute has already settled by the time this fires.
        public static event Action<string> OnBuildingBuilt;

        //  The tier the next copy is bought at: the last one starting at or below the current count.
        //  The final tier keeps applying once the count runs past every threshold, which is the same
        //  reading LandSystem takes of the copies already standing.
        public static BuildingCostTier GetNextCostTier(BuildingData building)
        {
            if (building?.Costs == null || building.Costs.Count == 0) return null;

            int count = ModifierValueSystem.GetBuildingCount(building.Name);
            var tiers = building.Costs.OrderBy(tier => tier.Count).ToList();

            //  A count below the first authored threshold still has to be priced, so the cheapest tier
            //  stands in for it.
            return tiers.LastOrDefault(tier => tier.Count <= count) ?? tiers[0];
        }

        //  Everything the next copy is authored to change, land included, for display.
        public static List<ResourceAmountData> GetNextPurchaseChanges(string buildingName)
        {
            var changes = new List<ResourceAmountData>();

            var tier = GetNextCostTier(BuildingDataSystem.GetBuildingData(buildingName));
            if (tier?.Cost == null) return changes;

            foreach (var cost in tier.Cost)
            {
                ResourceValue value = cost.GetValue();
                if (value == 0) continue;
                changes.Add(new ResourceAmountData(cost.ResourceType, value));
            }

            return changes;
        }

        //  Only what is actually spent out of the game state. Land is excluded: it is not a resource and
        //  so can neither be checked nor deducted as one.
        public static List<ResourceAmountData> GetNextPurchaseCost(string buildingName)
        {
            return GetNextPurchaseChanges(buildingName).Where(change => change.ResourceType != LandSystem.LandValueName).ToList();
        }

        public static int GetNextPurchaseLandCost(string buildingName)
        {
            return LandSystem.GetLandCostOfTier(GetNextCostTier(BuildingDataSystem.GetBuildingData(buildingName)));
        }

        public static bool CanBuild(string buildingName)
        {
            //  A building with no authored costs has no price to pay and so no purchase to make.
            if (GetNextCostTier(BuildingDataSystem.GetBuildingData(buildingName)) == null) return false;

            if (GetNextPurchaseLandCost(buildingName) > LandSystem.LandFree) return false;

            return GetNextPurchaseCost(buildingName).CheckCanChangeAll();
        }

        public static bool Build(string buildingName)
        {
            if (!CanBuild(buildingName)) return false;

            GameController.CurrentGameState.AddToResources(GetNextPurchaseCost(buildingName));

            //  The count change is what makes the building real: the modifier system accumulates the
            //  building's altered values from it, which in turn refreshes resource maxima, job caps,
            //  action modifiers and the land in use.
            ModifierValueSystem.AddBuilding(buildingName);

            OnBuildingBuilt?.Invoke(buildingName);
            return true;
        }
    }
}
