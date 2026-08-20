using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    [Serializable]
    public sealed class ActionTimingConfiguration
    {
        //  How many seconds of action progress pass per real second. This scales action progress alone
        //  rather than Unity's global Time.timeScale, so autosaving and UI animation keep real time.
        public float TimeScale = 1f;
        public int PathVisualizationWorkers = 10;
        public List<ActionTimingData> Actions = new();
    }

    [Serializable]
    public sealed class ActionTimingData
    {
        public string Name;
        public double ProgressMaximum;
        public double BaseProgressPerSecond;
        public double ProgressPerWorkerPerSecond;
        public SerializableDictionary<string, double> UnlockSpeedMultipliers = new();
        public List<string> AlterableValuePercentAdditions = new();

        public double CalculatePlayerProgressPerSecond(
            Func<string, bool> isUnlocked,
            Func<string, double> getAlterableValue)
        {
            double speed = BaseProgressPerSecond;

            if (UnlockSpeedMultipliers != null)
            {
                foreach (var alteration in UnlockSpeedMultipliers)
                    if (isUnlocked(alteration.Key))
                        speed *= alteration.Value;
            }

            double percentageMultiplier = 1.0;
            if (AlterableValuePercentAdditions != null)
            {
                foreach (string valueName in AlterableValuePercentAdditions)
                    if (!string.IsNullOrEmpty(valueName))
                        percentageMultiplier += getAlterableValue(valueName) * 0.01;
            }

            return speed * percentageMultiplier;
        }
    }
}
