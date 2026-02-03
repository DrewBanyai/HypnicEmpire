using System;

namespace HypnicEmpire
{
    public class ResourceAmountData
    {
        public ResourceAmountData(string resourceType, ResourceValue resourceValue)
        {
            ResourceType = resourceType;
            ResourceValue = resourceValue;
        }

        public string ResourceType;
        public ResourceValue ResourceValue;

        public bool CheckCanChange(bool allowPositivePartial = false)
        {
            if (ResourceValue == 0) return true;

            ResourceValue currentResourceAmount = GameController.CurrentGameState.GetResourceAmount(ResourceType);
            if (ResourceValue < 0) return currentResourceAmount >= ResourceValue.Abs();

            ResourceValue maxResourceAmount = GameController.CurrentGameState.GetResourceMaxAmount(ResourceType);
            return allowPositivePartial ? (currentResourceAmount < maxResourceAmount) : (currentResourceAmount + ResourceValue <= maxResourceAmount);
        }
    }
}