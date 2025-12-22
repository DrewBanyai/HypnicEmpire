using System.Collections.Generic;
using System;

namespace HypnicEmpire
{
    [Serializable]
    public class DevelopmentDataEntry
    {
        public List<string> Trigger;
        public string Title;
        public string Description;
        public string EffectText;
        public List<string> Unlock;
        public List<ResourceAmountData> Cost;
    }
    
    [Serializable]
    public class DevelopmentUnlockMultiplier
    {
        public string Unlock;
        public double Multiplier;
    }
    
    [Serializable]
    public class DevelopmentData
    {
        public List<DevelopmentUnlockMultiplier> CostMultiplierUnlocks;
        public List<DevelopmentDataEntry> DevelopmentEntries;
    }
}