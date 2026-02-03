namespace HypnicEmpire
{
    public class Localization
    {

        public static string DisplayText_PositiveNumberPlus(ResourceValue amount) => amount.Positive ? "+" : "";
        public static string DisplayText_ResourceDisplayName(string resourceType) => $"{resourceType}:";
        public static string DisplayText_ResourceChangeDisplayName(string resourceType) => $"{resourceType}";
        public static string DisplayText_ResourceChangeDisplayAmount(ResourceValue amount) => $"{DisplayText_PositiveNumberPlus(amount)}{amount.Text()}";
        public static string DisplayText_ResourceCountDivide(ResourceValue amount, ResourceValue max) => $"{amount.Text()} / {max.Text()}";
        public static string DisplayText_CurrentLevelAndMax(int currentLevel, int maxLevel) => $"{currentLevel} / {maxLevel}";
        public static string DisplayText_LevelExplorationPercent(int percent) => $"Level Exploration: {percent}%";

        public static string DisplayText_ActionName(string actionType) {
            return TaskActionSystem.TaskActionMap.ContainsKey(actionType) ? TaskActionSystem.TaskActionMap[actionType].DisplayName : "";
        }

        public static string JournalEntry_GameSaved() => "*GAME SAVED*";
    }
}