using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HypnicEmpire
{
    public static class GameUnlockSystem
    {
        private static Dictionary<GameUnlock, Action<bool>> UnlockActionMap = new();

        public static void AddGameUnlockAction(GameUnlock unlock, Action<bool> action) { UnlockActionMap[unlock] = action; }
        public static void SetUnlockValue(GameUnlock unlock, bool unlocked) { if (UnlockActionMap.ContainsKey(unlock)) UnlockActionMap[unlock](unlocked); }
    }
}