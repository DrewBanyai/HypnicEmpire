using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HypnicEmpire
{
    public class ResourceGameUnlockUtility
    {
        public static ResourceType? GetResourceTypeFromUnlock(GameUnlock unlock)
        {
            switch (unlock)
            {
                case GameUnlock.Unlocked_Resource_Food: return ResourceType.Food;
                case GameUnlock.Unlocked_Resource_Treasure: return ResourceType.Treasure;
                case GameUnlock.Unlocked_Resource_Herbs: return ResourceType.Herbs;
                case GameUnlock.Unlocked_Resource_Money: return ResourceType.Money;
                case GameUnlock.Unlocked_Resource_Wood: return ResourceType.Wood;
                case GameUnlock.Unlocked_Resource_People: return ResourceType.People;
            }
            return null;
        }

        public static GameUnlock? GetUnlockFromResourceType(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Food:     return GameUnlock.Unlocked_Resource_Food;
                case ResourceType.Treasure: return GameUnlock.Unlocked_Resource_Treasure;
                case ResourceType.Herbs: return GameUnlock.Unlocked_Resource_Herbs;
                case ResourceType.Money: return GameUnlock.Unlocked_Resource_Money;
                case ResourceType.Wood: return GameUnlock.Unlocked_Resource_Wood;
                case ResourceType.People: return GameUnlock.Unlocked_Resource_People;
            }
            return null;
        }
    }
}