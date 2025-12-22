using System.Collections.Generic;

namespace HypnicEmpire
{
    public class UnlockToActionData
    {
        public string Unlock;
        public string Action;
    }

    public class TaskActionData
    {
        public string Name;
        public string DisplayName;
        public string ActionSection;
        public double PlayerSpeed;
        public List<ResourceAmountData> ResourceChange;
    }

    public class TaskUnlockAndActionData
    {
        public List<UnlockToActionData> UnlockToActionMap;
        public List<TaskActionData> ActionData;
    }
}