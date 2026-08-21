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

        //  A reward the change will not go ahead without room for. A change is normally allowed while any one
        //  of its rewards has somewhere to go, which lets a full store be passed over in favour of the rest;
        //  a reward marked here is not one the player is willing to throw away, and its store being full
        //  closes the change off however much room the others have. Meaningless on a cost, which is judged
        //  on what is held rather than on what is free.
        public bool RequiresStorageSpace;

        //  Alterations, modifiers and display all work on copies so the loaded data is never mutated, and a
        //  copy has to carry everything the original said about the change rather than the amount alone.
        public ResourceAmountData Copy() { return CopyWithValue(ResourceValue); }

        public ResourceAmountData CopyWithValue(ResourceValue resourceValue)
        {
            return new ResourceAmountData(ResourceType, resourceValue) { RequiresStorageSpace = RequiresStorageSpace };
        }

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

            return GameController.CurrentGameState.ResourceStorageIsFull(ResourceType);
        }

        //  Whether this line raises no objection of its own to the change going ahead. Only a reward that
        //  insists on room can object, and only while the store it fills is full.
        public bool HasRequiredStorageSpace()
        {
            if (!RequiresStorageSpace) return true;

            return !RewardStorageIsFull();
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