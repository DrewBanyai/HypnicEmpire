using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    public static class GameUnlockSystem
    {
        private static Dictionary<GameUnlock, List<Action<bool>>> UnlockActionMap = new();

        public static void AddGameUnlockAction(GameUnlock unlock, Action<bool> action)
        {
            if (!UnlockActionMap.ContainsKey(unlock))
                UnlockActionMap[unlock] = new();
            UnlockActionMap[unlock].Add(action);
        }

        public static void SetUnlockValue(GameUnlock unlock, bool unlocked)
        {
            if (UnlockActionMap.ContainsKey(unlock))
                foreach (var action in UnlockActionMap[unlock])
                    action?.Invoke(unlocked);
        }
    }
}