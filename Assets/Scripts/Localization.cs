namespace HypnicEmpire
{
    public class Localization
    {

        public static string DisplayText_PositiveNumberPlus(int amount) => (amount > 0) ? "+" : "";
        public static string DisplayText_ResourceDisplayName(string resourceName) => $"{resourceName}:";
        public static string DisplayText_ResourceChangeDisplayName(string resourceName) => $"{resourceName}";
        public static string DisplayText_ResourceChangeDisplayAmount(int amount) => $"{DisplayText_PositiveNumberPlus(amount)}{amount}";
        public static string DisplayText_ResourceCountDivide(int amount, int max) => $"{amount} / {max}";
        public static string JournalEntry_GameSaved() => "*GAME SAVED*";
    }
}