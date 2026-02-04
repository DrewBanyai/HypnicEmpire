using System.Collections.Generic;

namespace HypnicEmpire
{
    public class UnlockToActionData
    {
        public string Unlock;
        public string Action;
    }

    public class TaskActionValueDeterminant
    {
        public double Efficiency;
        public double DefaultSpeed;
        public SerializableDictionary<string, double> UnlockMultipliers;
        public SerializableDictionary<string, double> UnlockAdditives;
        public List<string> AlterableValuePercentAdditions;

        public double GetSpeed(GameState gs)
        {
            double speed = DefaultSpeed;
            foreach (var entry in UnlockMultipliers) speed *= (gs.GetUnlockValue(entry.Key) ? entry.Value : 1.0);
            foreach (var entry in UnlockAdditives) speed += (gs.GetUnlockValue(entry.Key) ? entry.Value : 0.0);

            double percentageMultiplier = 1.0;
            foreach (string valName in AlterableValuePercentAdditions) percentageMultiplier += (AlterableValueSystem.GetAlterableValueCurrentVal(valName) * 0.01);
            speed *= percentageMultiplier;

            return speed;
        }
    }

    public class TaskActionData
    {
        public string Name;
        public string DisplayName;
        public string ActionSection;
        public TaskActionValueDeterminant ValueDeterminant;
        public List<ResourceAmountData> ResourceChange;
    }

    public class TaskUnlockAndActionData
    {
        public List<UnlockToActionData> UnlockToActionMap;
        public List<TaskActionData> ActionData;
    }
}