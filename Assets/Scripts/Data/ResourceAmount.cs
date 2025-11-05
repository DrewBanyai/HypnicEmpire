using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    [Serializable]
    public class ResourceAmount
    {
        public ResourceAmount(ResourceType rType, int amount)
        {
            ResourceType = rType;
            Amount = amount;
        }

        public ResourceType ResourceType;
        public int Amount;

        public bool CheckCanChange()
        {
            if (Amount == 0) return true;

            int currentResourceAmount = GameController.CurrentGameState.GetResourceAmount(ResourceType);
            if (Amount < 0) return currentResourceAmount >= Math.Abs(Amount);

            int maxResourceAmount = GameController.CurrentGameState.GetResourceMaxAmount(ResourceType);
            return currentResourceAmount + Amount <= maxResourceAmount;
        }
    }

    public static class ResourceAmountListExtension
    {
        public static void AddResourceAmount(this List<ResourceAmount> amountList, ResourceAmount add)
        {
            //  If no entry for the resource type exists in the list, add one with a value of 0
            if (!amountList.Any(ra => ra.ResourceType == add.ResourceType))
                amountList.Add(new ResourceAmount(add.ResourceType, 0));

            //  Find the entry for the given resource type and add the amount
            amountList.Find(ra => ra.ResourceType == add.ResourceType).Amount += add.Amount;
        }

        public static bool CheckCanChangeAny(this List<ResourceAmount> amountList)
        {
            foreach (ResourceAmount ra in amountList)
                if (ra.CheckCanChange()) return true;

            return false;
        }

        public static bool CheckCanChangeAll(this List<ResourceAmount> amountList)
        {
            foreach (ResourceAmount ra in amountList)
                if (!ra.CheckCanChange()) return false;

            return true;
        }

        public static void ExecuteChange(this List<ResourceAmount> amountList)
        {
            foreach (ResourceAmount ra in amountList)
                GameController.CurrentGameState.AddToResource(ra.ResourceType, ra.Amount);
        }

        public static bool IsIdentical(this List<ResourceAmount> amountList, List<ResourceAmount> otherList)
        {
            if (amountList.Count != otherList.Count) return false;
            foreach (var entry in amountList)
            {
                var foundEntry = otherList.Find(e => e.ResourceType == entry.ResourceType);
                if (foundEntry == null || foundEntry.Amount != entry.Amount) return false;
            }

            return true;
        }
    }
}