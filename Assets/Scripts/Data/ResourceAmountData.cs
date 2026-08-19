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

        //  A reward none of which can land, because the store it fills is already at its maximum. A reward
        //  that only partly fits still moves the stockpile, so it is not counted here.
        public bool RewardStorageIsFull()
        {
            if (ResourceValue <= 0) return false;

            return GameController.CurrentGameState.GetResourceAmount(ResourceType) >= GameController.CurrentGameState.GetResourceMaxAmount(ResourceType);
        }

        //  A cost no amount of saving can meet, because more is asked for than the resource is able to
        //  hold at all. Raising that maximum is the only way through, so this is a different answer from
        //  CheckCanChange refusing a cost the player has simply not yet stockpiled.
        public bool ExceedsResourceCapacity()
        {
            if (ResourceValue >= 0) return false;

            return ResourceValue.Abs() > GameController.CurrentGameState.GetResourceMaxAmount(ResourceType);
        }
    }
}