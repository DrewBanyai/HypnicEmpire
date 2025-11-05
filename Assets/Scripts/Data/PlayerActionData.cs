using UnityEngine;
using System.Collections.Generic;
using System;

namespace HypnicEmpire
{
    [Serializable]
    public class PlayerActionData
    {
        public PlayerActionType ActionType;
        public PlayerActionSectionType SectionType;
        public List<ResourceAmount> ResourceChange;
    }
}