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

        public static string JournalEntry_GameSaved() => "*GAME SAVED*";
    }
}