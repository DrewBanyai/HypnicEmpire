namespace HypnicEmpire
{
    public class Localization
    {

        public static string DisplayText_PositiveNumberPlus(int amount) => (amount > 0) ? "+" : "";
        public static string DisplayText_ResourceDisplayName(string resourceType) => $"{resourceType}:";
        public static string DisplayText_ResourceChangeDisplayName(string resourceType) => $"{resourceType}";
        public static string DisplayText_ResourceChangeDisplayAmount(int amount) => $"{DisplayText_PositiveNumberPlus(amount)}{amount}";
        public static string DisplayText_ResourceCountDivide(int amount, int max) => $"{amount} / {max}";
        public static string DisplayText_CurrentLevelAndMax(int currentLevel, int maxLevel) => $"{currentLevel} / {maxLevel}";
        public static string DisplayText_LevelExplorationPercent(int percent) => $"Level Exploration: {percent}%";

        public static string DisplayText_ActionName(string actionType) {
            return TaskActionSystem.TaskActionMap.ContainsKey(actionType) ? TaskActionSystem.TaskActionMap[actionType].DisplayName : "";
        }

        public static string JournalEntry_GameSaved() => "*GAME SAVED*";
    }
}