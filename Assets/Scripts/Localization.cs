namespace HypnicEmpire
{
    public class Localization
    {

        public static string DisplayText_PositiveNumberPlus(int amount) => (amount > 0) ? "+" : "";
        public static string DisplayText_ResourceDisplayName(ResourceType resourceType) => $"{resourceType.ToString()}:";
        public static string DisplayText_ResourceChangeDisplayName(ResourceType resourceType) => $"{resourceType.ToString()}";
        public static string DisplayText_ResourceChangeDisplayAmount(int amount) => $"{DisplayText_PositiveNumberPlus(amount)}{amount}";
        public static string DisplayText_ResourceCountDivide(int amount, int max) => $"{amount} / {max}";
        public static string DisplayText_CurrentLevelAndMax(int currentLevel, int maxLevel) => $"{currentLevel + 1} / {maxLevel + 1}";
        public static string DisplayText_LevelExplorationPercent(int percent) => $"Level Exploration: {percent}%";

        public static string DisplayText_ActionName(PlayerActionType actionType)
        {
            switch (actionType)
            {
                case PlayerActionType.Delve: return "Delve";
                case PlayerActionType.Forage: return "Forage";
                case PlayerActionType.Hunting: return "Hunting";
                case PlayerActionType.Sell_Food: return "Sell Food";
                case PlayerActionType.Trade_Herbs: return "Trade Herbs";
                case PlayerActionType.Market: return "Market";
                case PlayerActionType.Chop_Wood: return "Chop Wood";
            }
            return "Unknown Action";
        }

        public static string JournalEntry_GameSaved() => "*GAME SAVED*";
    }
}