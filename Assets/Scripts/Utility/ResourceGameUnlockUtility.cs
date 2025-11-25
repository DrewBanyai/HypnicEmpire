using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HypnicEmpire
{
    public class ResourceGameUnlockUtility
    {
        public static ResourceType? GetResourceTypeFromUnlock(string unlock)
        {
            switch (unlock)
            {
                case "Unlock_Resource_Food": return ResourceType.Food;
                case "Unlock_Resource_Treasure": return ResourceType.Treasure;
                case "Unlock_Resource_Herbs": return ResourceType.Herbs;
                case "Unlock_Resource_Money": return ResourceType.Money;
                case "Unlock_Resource_Wood": return ResourceType.Wood;
                case "Unlock_Resource_People": return ResourceType.People;
            }
            return null;
        }

        public static string? GetUnlockFromResourceType(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Food:     return "Unlock_Resource_Food";
                case ResourceType.Treasure: return "Unlock_Resource_Treasure";
                case ResourceType.Herbs: return "Unlock_Resource_Herbs";
                case ResourceType.Money: return "Unlock_Resource_Money";
                case ResourceType.Wood: return "Unlock_Resource_Wood";
                case ResourceType.People: return "Unlock_Resource_People";
            }
            return null;
        }
    }
}