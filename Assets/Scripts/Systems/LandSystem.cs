using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    //  Land is bought without limit as long as the money is there, and is consumed by the buildings
    //  that stand on it. Owned land is the authored starting amount plus everything acquired since;
    //  used land is derived from the current building counts, so the two can never drift apart.
    //  The "Land" AlterableValue mirrors owned land so its value unlocks keep firing.
    public static class LandSystem
    {
        public const string LandValueName = "Land";

        //  Raised whenever owned or used land changes.
        public static event Action OnLandChanged;

        private static int StartingLandOwned;
        private static int UsedLand;
        private static bool Initialized;

        public static int LandOwned => StartingLandOwned + GameController.CurrentGameState.LandAcquired;
        public static int LandUsed => UsedLand;
        public static int LandFree => LandOwned - UsedLand;

        public static void Initialize()
        {
            if (!Initialized)
            {
                //  Snapshot the authored value BEFORE the mirror below ever writes to it, otherwise a
                //  later initialize would fold already acquired land into the starting amount.
                StartingLandOwned = AlterableValueSystem.GetAlterableValueCurrentVal(LandValueName);

                //  Building counts are owned by the modifier system, so used land follows its recomputes.
                ModifierValueSystem.OnValuesRecomputed += Refresh;
                Initialized = true;
            }

            Refresh();
        }

        //  Both halves of the calculation change outside this system (buildings are built, land is
        //  bought, a save is loaded or the game is reset), so recalculating is always safe to call.
        public static void Refresh()
        {
            UsedLand = CalculateLandUsed();
            MirrorOwnedLandToAlterableValue();
            OnLandChanged?.Invoke();
        }

        //  Every resource actually spent to buy land. The land gained by the purchase is excluded: it
        //  is not held in the game state and so cannot be checked or spent as a resource.
        public static List<ResourceAmountData> GetLandPurchaseCost()
        {
            return GetLandPurchaseChanges().Where(change => change.ResourceType != LandValueName).ToList();
        }

        //  The full authored change of a land purchase, cost and reward alike, for display.
        public static List<ResourceAmountData> GetLandPurchaseChanges()
        {
            var changes = new List<ResourceAmountData>();
            if (BuildingDataSystem.Data?.LandCost == null) return changes;

            foreach (var cost in BuildingDataSystem.Data.LandCost)
            {
                ResourceValue value = cost.GetValue();
                if (value == 0) continue;
                changes.Add(new ResourceAmountData(cost.ResourceType, value));
            }

            return changes;
        }

        public static int GetLandPurchaseAmount()
        {
            int amount = 0;
            if (BuildingDataSystem.Data?.LandCost == null) return amount;

            foreach (var cost in BuildingDataSystem.Data.LandCost)
                if (cost.ResourceType == LandValueName)
                    amount += (int)cost.GetValue().WholeValue;

            return amount;
        }

        public static bool CanBuyLand()
        {
            return GetLandPurchaseAmount() > 0 && GetLandPurchaseCost().CheckCanChangeAll();
        }

        public static void BuyLand()
        {
            if (!CanBuyLand()) return;

            GameController.CurrentGameState.AddToResources(GetLandPurchaseCost());
            AddLand(GetLandPurchaseAmount());
        }

        public static void AddLand(int amount)
        {
            if (amount == 0) return;

            GameController.CurrentGameState.LandAcquired = Math.Max(0, GameController.CurrentGameState.LandAcquired + amount);
            Refresh();
        }

        private static void MirrorOwnedLandToAlterableValue()
        {
            if (!AlterableValueSystem.ValueMap.TryGetValue(LandValueName, out var landValue)) return;
            landValue.SetValue(LandOwned);
        }

        private static int CalculateLandUsed()
        {
            var buildings = BuildingDataSystem.Data?.BuildingTypes;
            if (buildings == null) return 0;

            int used = 0;
            foreach (var building in buildings)
                used += GetLandUsedByBuilding(building);

            return used;
        }

        //  A building's cost changes with how many of it already stand, so the land it occupies is the
        //  sum of the land each individual copy cost at the tier it was built in.
        private static int GetLandUsedByBuilding(BuildingData building)
        {
            int count = ModifierValueSystem.GetBuildingCount(building.Name);
            if (count <= 0 || building.Costs == null) return 0;

            var tiers = building.Costs.OrderBy(tier => tier.Count).ToList();
            int used = 0;
            for (int i = 0; i < tiers.Count; i++)
            {
                int tierStart = tiers[i].Count;
                if (tierStart >= count) break;

                int tierEnd = (i + 1 < tiers.Count) ? Math.Min(tiers[i + 1].Count, count) : count;
                used += (tierEnd - tierStart) * GetLandCostOfTier(tiers[i]);
            }

            return used;
        }

        //  The land one copy of a building occupies at a given cost tier. Land is authored as a cost
        //  like any other resource, so reading it out of a tier belongs here rather than with whoever
        //  is pricing the purchase.
        public static int GetLandCostOfTier(BuildingCostTier tier)
        {
            if (tier?.Cost == null) return 0;

            int land = 0;
            foreach (var cost in tier.Cost)
                if (cost.ResourceType == LandValueName)
                    land += (int)cost.GetValue().Abs().WholeValue;

            return land;
        }
    }
}
