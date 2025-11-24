using System;
using System.Collections.Generic;
using UnityEngine;

namespace HypnicEmpire
{
    public class LevelGroupingLoaded
    {
        public int Min;
        public int Max;
        public string Name;
    }

    public class LevelGrouping
    {
        public int Min;
        public int Max;
        public string Name;
        public string Image;
        public int Difficulty;
        public List<ResourceAmountData> ResourceChange;
        public Sprite ImageSprite;
    }

    public class LevelDataEntry
    {
        public int Level;
        public string Description;
        public int DelveCount;
        public string Unlock;
    }

    public class LevelData
    {
        public List<LevelGrouping> LevelGroupings;
        public List<LevelDataEntry> LevelDataEntries;
    }
}