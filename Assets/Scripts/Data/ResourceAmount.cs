using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public static class ResourceAmountListExtension
    {
        public static void AddResourceAmount(this List<ResourceAmountData> amountList, ResourceAmountData add)
        {
            //  If no entry for the resource type exists in the list, add one with a value of 0
            if (!amountList.Any(ra => ra.ResourceType == add.ResourceType))
                amountList.Add(new ResourceAmountData(add.ResourceType, 0));

            //  Find the entry for the given resource type and add the amount
            amountList.Find(ra => ra.ResourceType == add.ResourceType).ResourceValue += add.ResourceValue;
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