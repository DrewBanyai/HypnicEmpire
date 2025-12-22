using System;

namespace HypnicEmpire
{
    public class ResourceAmountData
    {
        public ResourceAmountData(string resourceType, int amount)
        {
            ResourceType = resourceType;
            Amount = amount;
        }

        public string ResourceType;
        public int Amount;

        public bool CheckCanChange(bool allowPositivePartial = false)
        {
            if (Amount == 0) return true;

            int currentResourceAmount = GameController.CurrentGameState.GetResourceAmount(ResourceType);
            if (Amount < 0) return currentResourceAmount >= Math.Abs(Amount);

            int maxResourceAmount = GameController.CurrentGameState.GetResourceMaxAmount(ResourceType);
            return allowPositivePartial ? (currentResourceAmount < maxResourceAmount) : (currentResourceAmount + Amount <= maxResourceAmount);
        }
    }
}