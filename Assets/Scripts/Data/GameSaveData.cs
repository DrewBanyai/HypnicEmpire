using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HypnicEmpire
{
    [Serializable]
    public class GameSaveData
    {
        public GameState GameState;
        public SerializableDictionary<string, bool> GameUnlockList;
        public SerializableDictionary<string, int> BuildingCounts;
        public SerializableDictionary<string, int> WorkerAssignments;
        public List<string> UnlockedAchievements;
    }
}