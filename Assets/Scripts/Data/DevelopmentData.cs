using UnityEngine;
using System.Collections.Generic;
using System;

namespace HypnicEmpire
{
    [Serializable]
    public class DevelopmentData
    {
        public GameUnlock Trigger;
        public string Title;
        public string Description;
        public string EffectText;
        public GameUnlock Unlock;
        public List<ResourceAmount> Cost;
    }
}