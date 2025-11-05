using UnityEngine;
using System.Collections.Generic;
using System;

namespace HypnicEmpire
{
    [Serializable]
    public class GameLevelData
    {
        public Sprite Sprite;
        public string Name;
        public string Description;
        public SerializableDictionary<ResourceType, int> ResourceChanges = new();
        public int DelveCount = 99;
    }
}