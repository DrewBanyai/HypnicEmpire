using System.Collections.Generic;

namespace HypnicEmpire
{
    public static class ResourceAmountListExtension
    {
        public static void AddResourceAmount(this List<ResourceAmountData> amountList, ResourceAmountData add)
        {
            //  Merging into an existing entry keeps the stricter of the two demands for room: one contribution
            //  insisting the reward not be thrown away speaks for the merged line as well.
            ResourceAmountData existing = amountList.Find(ra => ra.ResourceType == add.ResourceType);
            if (existing == null)
            {
                amountList.Add(add.Copy());
                return;
            }

            existing.ResourceValue += add.ResourceValue;
            existing.RequiresStorageSpace |= add.RequiresStorageSpace;
        }

        public static bool CheckCanChangeAny(this List<ResourceAmountData> amountList, bool allowPositivePartial = false)
        {
            foreach (var ra in amountList)
                if (ra.CheckCanChange(allowPositivePartial)) return true;

            return false;
        }

        public static bool CheckCanChangeAll(this List<ResourceAmountData> amountList, bool allowPositivePartial = false)
        {
            foreach (var ra in amountList)
                if (!ra.CheckCanChange(allowPositivePartial)) return false;

            return true;
        }

        //  A single reward that insists on room and has none is enough to hold the whole change back, whatever
        //  room the rest of the rewards have. Rewards that make no such demand say nothing either way here.
        public static bool CheckRequiredStorageSpaceAll(this List<ResourceAmountData> amountList)
        {
            foreach (var ra in amountList)
                if (!ra.HasRequiredStorageSpace()) return false;

            return true;
        }

        //  A single resource asked for beyond what it can hold is enough to put the whole change out of
        //  reach, however long the player saves towards the rest of it.
        public static bool ExceedsResourceCapacityAny(this List<ResourceAmountData> amountList)
        {
            foreach (var ra in amountList)
                if (ra.ExceedsResourceCapacity()) return true;

            return false;
        }

        public static void ExecuteChange(this List<ResourceAmountData> amountList)
        {
            foreach (ResourceAmountData ra in amountList)
                GameController.CurrentGameState.AddToResource(ra.ResourceType, ra.ResourceValue);
        }

        public static bool IsIdentical(this List<ResourceAmountData> amountList, List<ResourceAmountData> otherList)
        {
            if (amountList.Count != otherList.Count) return false;
            foreach (var entry in amountList)
            {
                var foundEntry = otherList.Find(e => e.ResourceType == entry.ResourceType);
                if (foundEntry == null || foundEntry.ResourceValue != entry.ResourceValue) return false;
            }

            return true;
        }
    }
}