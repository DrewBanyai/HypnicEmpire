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

    [Serializable]
    public class ResourceAmountCollection
    {
        public List<ResourceAmount> ResourceAmounts;

        public ResourceAmountCollection()
        {
            ResourceAmounts = new List<ResourceAmount>();
        }
        
        public ResourceAmountCollection(ResourceAmountCollection other)
        {
            ResourceAmounts = new List<ResourceAmount>();
            foreach (ResourceAmount otherResourceAmount in other.ResourceAmounts)
                ResourceAmounts.Add(new ResourceAmount(otherResourceAmount.ResourceType, otherResourceAmount.Amount));
        }

        public void AddResourceAmount(ResourceAmount addedAmount)
        {
            if (!ResourceAmounts.Any(ra => ra.ResourceType == addedAmount.ResourceType))
                ResourceAmounts.Add(new ResourceAmount(addedAmount.ResourceType, addedAmount.Amount));
            else
                ResourceAmounts.Find(ra => ra.ResourceType == addedAmount.ResourceType).Amount += addedAmount.Amount;
        }

        public bool CheckCanChange()
        {
            foreach (ResourceAmount ar in ResourceAmounts)
                if (!ar.CheckCanChange()) return false;

            return true;
        }

        public void ExecuteChange()
        {
            foreach (ResourceAmount ar in ResourceAmounts)
                GameController.CurrentGameState.AddToResource(ar.ResourceType, ar.Amount);
        }
    }
}