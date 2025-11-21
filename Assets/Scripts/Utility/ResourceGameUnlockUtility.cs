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
                case GameUnlock.Unlock_Resource_Food: return ResourceType.Food;
                case GameUnlock.Unlock_Resource_Treasure: return ResourceType.Treasure;
                case GameUnlock.Unlock_Resource_Herbs: return ResourceType.Herbs;
                case GameUnlock.Unlock_Resource_Money: return ResourceType.Money;
                case GameUnlock.Unlock_Resource_Wood: return ResourceType.Wood;
                case GameUnlock.Unlock_Resource_People: return ResourceType.People;
            }
            return null;
        }

        public static GameUnlock? GetUnlockFromResourceType(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Food:     return GameUnlock.Unlock_Resource_Food;
                case ResourceType.Treasure: return GameUnlock.Unlock_Resource_Treasure;
                case ResourceType.Herbs: return GameUnlock.Unlock_Resource_Herbs;
                case ResourceType.Money: return GameUnlock.Unlock_Resource_Money;
                case ResourceType.Wood: return GameUnlock.Unlock_Resource_Wood;
                case ResourceType.People: return GameUnlock.Unlock_Resource_People;
            }
            return null;
        }
    }
}